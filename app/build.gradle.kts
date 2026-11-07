plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.mrjackspade.deaddit"
    compileSdk = 36
    ndkVersion = "28.2.13676358"

    defaultConfig {
        // The MAUI app's ID: this app replaces it in place and takes over its data (MauiMigration).
        applicationId = "com.companyname.deaddit"
        minSdk = 26
        targetSdk = 36
        // Published builds pass -PdeadditVersion=X.Y.Z or X.Y.Z-preview-N (from the git tags).
        val version = (findProperty("deadditVersion") as String?) ?: "0.0.0-dev"
        versionName = version
        versionCode = versionCodeOf(version)

        // The ABIs native/build-android.sh builds libredditapi.so for: phones, and the dev emulator.
        ndk {
            abiFilters += listOf("arm64-v8a", "x86_64")
        }
    }

    externalNativeBuild {
        cmake {
            path = file("src/main/cpp/CMakeLists.txt")
        }
    }

    // Release builds are signed with the keystore from the repository secrets (see publish.yml).
    signingConfigs {
        create("release") {
            if (gradle.startParameter.taskNames.any { it.contains("Release") }) {
                storeFile = file(requiredEnv("DEADDIT_KEYSTORE"))
                storePassword = requiredEnv("DEADDIT_STORE_PASS")
                keyAlias = requiredEnv("DEADDIT_KEY_ALIAS")
                keyPassword = requiredEnv("DEADDIT_KEY_PASS")
            }
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            signingConfig = signingConfigs.getByName("release")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }
}

kotlin {
    jvmToolchain(17)
}

dependencies {
    implementation(project(":core"))

    implementation(platform(libs.compose.bom))
    implementation(libs.compose.ui)
    implementation(libs.compose.material3)
    implementation(libs.compose.icons)
    debugImplementation(libs.compose.tooling)
    implementation(libs.activity.compose)
    implementation(libs.lifecycle.viewmodel.compose)
    implementation(libs.lifecycle.runtime.compose)
    implementation(libs.navigation.compose)
    implementation(libs.coroutines.android)
    implementation(libs.okhttp)
    implementation(libs.coil.compose)
    implementation(libs.coil.network)
    implementation(libs.coil.gif)
    implementation(libs.media3.exoplayer)
    implementation(libs.media3.dash)
    implementation(libs.media3.hls)
    implementation(libs.media3.ui)
}

/**
 * X.Y.Z-preview-N → X YY ZZZZ NN (digits), with a release (no suffix) as NN = 99, so each release sorts after
 * its previews and Android accepts every newer build as an update.
 */
fun versionCodeOf(version: String): Int {
    val match = Regex("""^(\d+)\.(\d+)\.(\d+)(?:-preview-(\d+))?$|^0\.0\.0-dev$""").matchEntire(version)
        ?: throw GradleException("Version '$version' isn't X.Y.Z or X.Y.Z-preview-N.")
    if (version == "0.0.0-dev") return 1
    val (major, minor, build, preview) = match.destructured
    require(major.toInt() < 21 && minor.toInt() < 100 && build.toInt() < 10000) { "Version '$version' doesn't fit the version code scheme." }
    val previewNumber = if (preview.isEmpty()) 99 else preview.toInt().also { require(it in 1..98) { "Too many previews of $version." } }
    return ((major.toInt() * 100 + minor.toInt()) * 10000 + build.toInt()) * 100 + previewNumber
}

fun requiredEnv(name: String): String =
    System.getenv(name) ?: throw GradleException("$name must be set to build a release.")
