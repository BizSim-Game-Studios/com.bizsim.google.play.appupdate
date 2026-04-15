# UnityPlayerActivity → FragmentActivity override

**When you need this:** Classic Unity's `UnityPlayerActivity` (not Unity 6 `GameActivity`).

**Unity 6 `GameActivity` users:** Skip this file. `GameActivity` extends `FragmentActivity` natively.

## Step 1: `Assets/Plugins/Android/BizSimAppUpdateActivity.java`

Replace `com.example.yourgame` with your application id.

```java
package com.example.yourgame;

import android.content.Intent;
import android.content.res.Configuration;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.WindowManager;
import androidx.fragment.app.FragmentActivity;
import com.unity3d.player.UnityPlayer;

public class BizSimAppUpdateActivity extends FragmentActivity {
    protected UnityPlayer mUnityPlayer;

    @Override protected void onCreate(Bundle savedInstanceState) {
        requestWindowFeature(android.view.Window.FEATURE_NO_TITLE);
        super.onCreate(savedInstanceState);
        getWindow().setFormat(android.graphics.PixelFormat.RGBX_8888);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        mUnityPlayer = new UnityPlayer(this);
        setContentView(mUnityPlayer);
        mUnityPlayer.requestFocus();
    }

    @Override protected void onNewIntent(Intent intent)        { setIntent(intent); mUnityPlayer.newIntent(intent); }
    @Override protected void onResume()                         { super.onResume(); mUnityPlayer.resume(); }
    @Override protected void onPause()                          { super.onPause(); mUnityPlayer.pause(); }
    @Override protected void onStart()                          { super.onStart(); mUnityPlayer.start(); }
    @Override protected void onStop()                           { super.onStop(); mUnityPlayer.stop(); }
    @Override protected void onDestroy()                        { mUnityPlayer.destroy(); super.onDestroy(); }
    @Override public void onConfigurationChanged(Configuration c) { super.onConfigurationChanged(c); mUnityPlayer.configurationChanged(c); }
    @Override public void onWindowFocusChanged(boolean f)       { super.onWindowFocusChanged(f); mUnityPlayer.windowFocusChanged(f); }
    @Override public boolean dispatchKeyEvent(KeyEvent e)       { if (e.getAction() == KeyEvent.ACTION_MULTIPLE) return mUnityPlayer.injectEvent(e); return super.dispatchKeyEvent(e); }
    @Override public boolean onKeyUp(int kc, KeyEvent e)        { return mUnityPlayer.injectEvent(e); }
    @Override public boolean onKeyDown(int kc, KeyEvent e)      { return mUnityPlayer.injectEvent(e); }
    @Override public boolean onTouchEvent(MotionEvent e)        { return mUnityPlayer.injectEvent(e); }
    @Override public boolean onGenericMotionEvent(MotionEvent e){ return mUnityPlayer.injectEvent(e); }
}
```

## Step 2: `Assets/Plugins/Android/AndroidManifest.xml`

Override Unity's default manifest activity entry:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:tools="http://schemas.android.com/tools"
    package="com.example.yourgame">
  <application>
    <activity
        android:name=".BizSimAppUpdateActivity"
        android:label="@string/app_name"
        android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density"
        android:launchMode="singleTask"
        android:screenOrientation="fullUser"
        android:hardwareAccelerated="true"
        tools:replace="android:name">
      <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
        <category android:name="android.intent.category.LEANBACK_LAUNCHER" />
      </intent-filter>
      <meta-data android:name="unityplayer.UnityActivity" android:value="true" />
    </activity>
  </application>
</manifest>
```

## Step 3: Verify

Build Android. Check `Temp/gradleOut/launcher/src/main/AndroidManifest.xml` for `com.example.yourgame.BizSimAppUpdateActivity` (not `com.unity3d.player.UnityPlayerActivity`). Run the app. `AppUpdateConfiguration` window should show a green compatibility banner.

## Alternative: Unity 6 `GameActivity`

Player Settings → Android → Other Settings → Application Entry Point → `GameActivity`. No override needed.
