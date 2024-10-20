# How to install
Android APK file is available on the [Releases](/../../releases) page. Download the APK file and install it on your Android device.

Windows client can be built from scratch. Simply pull the code and build.

IOS is not currently enabled as I don't have an IOS device to test on. If you have an IOS device and would like to test, please let me know and I will enable the IOS build.

# How to Get Your Reddit API Credentials

To use this custom Reddit client, you need to acquire the following credentials:
- **Username**: Your Reddit username
- **Password**: Your Reddit account password
- **ApiKey**: A Reddit API Key
- **ApiSecret**: A Reddit API Secret

Follow the steps below to obtain your `ApiKey` and `ApiSecret`:

### Step 1: Log into Reddit
If you don’t have a Reddit account yet, [sign up here](https://www.reddit.com/register/). After creating your account, log in.

### Step 2: Create a Reddit App
1. Go to [Reddit's App Preferences](https://www.reddit.com/prefs/apps) (You must be logged in).
2. Scroll down to the **Developed Applications** section and click the **Create App** button.
   
### Step 3: Fill in the Application Details
In the form that appears:
- **Name**: Choose a name for your application (e.g., `My Reddit Client`).
- **App type**: Select `script` (this is for personal use/local scripts).
- **Description**: (Optional) Add a description, though it's not required.
- **Redirect URI**: Use `http://localhost:8080` as the redirect URI (this is a placeholder; you won’t need it for a script app).
- **Permissions**: No special permissions are needed; the defaults should work.

Click **Create app** at the bottom.

### Step 4: Get Your API Credentials
After creating the app, you’ll see a section with your app’s details:
- **Client ID**: This is your `ApiKey`. It’s located under the app name (a string of letters/numbers).
- **Client Secret**: This is your `ApiSecret`. It's labeled as **secret**.

### Step 5: Save Your Credentials
In order to use this app, you need to save the following credentials:
- **Username**: Your Reddit username.
- **Password**: Your Reddit account password.
- **ApiKey**: The `Client ID` from the app details.
- **ApiSecret**: The `Client Secret` from the app details.

Once you have these details, you’re ready to configure the client.

