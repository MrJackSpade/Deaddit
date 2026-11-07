/**
 * Runs core's unit tests on an Android device (`:core-device-tests:connectedDebugAndroidTest`). Android's
 * java.util.regex, java.text and friends are ICU-backed and differ from the desktop JVM's that `:core:test` uses,
 * and core's code runs on Android. Core's sources compile here as this module's own, so the tests see its internals.
 */
plugins {
    alias(libs.plugins.android.library)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.mrjackspade.deaddit.core.devicetests"
    compileSdk = 36

    defaultConfig {
        minSdk = 26
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    sourceSets["main"].java.srcDir("../core/src/main/kotlin")
    sourceSets["main"].resources.srcDir("../core/src/main/resources")
    sourceSets["androidTest"].java.srcDir("../core/src/test/kotlin")
    sourceSets["androidTest"].resources.srcDir("../core/src/test/resources")

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
}

kotlin {
    jvmToolchain(17)
}

dependencies {
    implementation(libs.coroutines.core)
    implementation(libs.serialization.json)
    implementation(libs.jsoup)

    androidTestImplementation(libs.junit)
    androidTestImplementation(libs.androidx.test.runner)
}
