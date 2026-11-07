// Connects Kotlin to libredditapi.so (Reddit.Api compiled natively, see native/).
// Kotlin -> .NET: init/call below. .NET -> Kotlin: the transport and refresh callbacks, which call
// static methods on NativeRedditApi. Everything crossing is UTF-8 JSON passed as byte arrays,
// because JNI's own string conversion mangles characters outside the BMP (emoji).
#include <jni.h>
#include <stdlib.h>
#include <string.h>

extern char *ra_init(char *(*transport)(const char *), char *(*refresh_session)(void), const char *user_agent);
extern char *ra_call(const char *request_json);

static JavaVM *g_vm;
static jclass g_class;
static jmethodID g_send;
static jmethodID g_refresh;

static JNIEnv *env_for_thread(void) {
    JNIEnv *env;
    if ((*g_vm)->GetEnv(g_vm, (void **) &env, JNI_VERSION_1_6) == JNI_OK) {
        return env;
    }
    // .NET thread pool threads: attach once and leave attached.
    if ((*g_vm)->AttachCurrentThreadAsDaemon(g_vm, &env, NULL) != JNI_OK) {
        abort();
    }
    return env;
}

static jbyteArray to_java(JNIEnv *env, const char *text) {
    jsize length = (jsize) strlen(text);
    jbyteArray array = (*env)->NewByteArray(env, length);
    (*env)->SetByteArrayRegion(env, array, 0, length, (const jbyte *) text);
    return array;
}

// Copies a Java byte[] into a NUL-terminated malloc buffer, which .NET frees.
static char *to_native(JNIEnv *env, jbyteArray array) {
    if (array == NULL) {
        return NULL;
    }
    jsize length = (*env)->GetArrayLength(env, array);
    char *text = malloc((size_t) length + 1);
    (*env)->GetByteArrayRegion(env, array, 0, length, (jbyte *) text);
    text[length] = 0;
    return text;
}

static void fail_on_exception(JNIEnv *env, const char *where) {
    if ((*env)->ExceptionCheck(env)) {
        // The Kotlin callbacks catch everything themselves, so this is a bug: fail loudly.
        (*env)->ExceptionDescribe(env);
        (*env)->FatalError(env, where);
    }
}

static char *transport(const char *request_json) {
    JNIEnv *env = env_for_thread();
    jbyteArray request = to_java(env, request_json);
    jbyteArray response = (*env)->CallStaticObjectMethod(env, g_class, g_send, request);
    fail_on_exception(env, "NativeRedditApi.send threw");
    char *result = to_native(env, response);
    (*env)->DeleteLocalRef(env, request);
    (*env)->DeleteLocalRef(env, response);
    return result;
}

static char *refresh_session(void) {
    JNIEnv *env = env_for_thread();
    jbyteArray response = (*env)->CallStaticObjectMethod(env, g_class, g_refresh);
    fail_on_exception(env, "NativeRedditApi.refreshSession threw");
    char *result = to_native(env, response);
    if (response != NULL) {
        (*env)->DeleteLocalRef(env, response);
    }
    return result;
}

JNIEXPORT jint JNI_OnLoad(JavaVM *vm, void *reserved) {
    g_vm = vm;
    JNIEnv *env;
    (*vm)->GetEnv(vm, (void **) &env, JNI_VERSION_1_6);
    jclass local = (*env)->FindClass(env, "com/mrjackspade/deaddit/api/NativeRedditApi");
    g_class = (*env)->NewGlobalRef(env, local);
    g_send = (*env)->GetStaticMethodID(env, g_class, "send", "([B)[B");
    g_refresh = (*env)->GetStaticMethodID(env, g_class, "refreshSession", "()[B");
    if (g_send == NULL || g_refresh == NULL) {
        return JNI_ERR;
    }
    return JNI_VERSION_1_6;
}

// Returns null on success, or the error JSON.
JNIEXPORT jbyteArray JNICALL
Java_com_mrjackspade_deaddit_api_NativeRedditApi_nativeInit(JNIEnv *env, jclass cls, jbyteArray user_agent) {
    char *agent = to_native(env, user_agent);
    char *error = ra_init(transport, refresh_session, agent);
    free(agent);
    if (error == NULL) {
        return NULL;
    }
    jbyteArray result = to_java(env, error);
    free(error);
    return result;
}

JNIEXPORT jbyteArray JNICALL
Java_com_mrjackspade_deaddit_api_NativeRedditApi_nativeCall(JNIEnv *env, jclass cls, jbyteArray request_json) {
    char *request = to_native(env, request_json);
    char *response = ra_call(request);
    free(request);
    jbyteArray result = to_java(env, response);
    free(response);
    return result;
}
