plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.serialization)
}

kotlin {
    jvmToolchain(17)
}

dependencies {
    api(libs.coroutines.core)
    api(libs.serialization.json)
    implementation(libs.jsoup)

    testImplementation(libs.junit)
}

tasks.test {
    systemProperty("deaddit.live", System.getProperty("deaddit.live") ?: "false")
    testLogging { events("passed", "failed", "skipped"); showStandardStreams = true; exceptionFormat = org.gradle.api.tasks.testing.logging.TestExceptionFormat.FULL }
}
