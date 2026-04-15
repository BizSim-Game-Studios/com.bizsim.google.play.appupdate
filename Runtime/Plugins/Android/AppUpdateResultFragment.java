package com.bizsim.google.play.appupdate;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import androidx.fragment.app.Fragment;
import androidx.fragment.app.FragmentActivity;
import androidx.fragment.app.FragmentManager;

import com.google.android.play.core.install.model.ActivityResult;

public final class AppUpdateResultFragment extends Fragment {
    private static final String TAG = "BizSimAppUpdateFrag";

    private AppUpdateBridge.IFlowLaunchCallback pendingCallback;

    public static AppUpdateResultFragment attachTo(Activity host) {
        if (!(host instanceof FragmentActivity)) {
            throw new IllegalStateException(
                "AppUpdateResultFragment requires FragmentActivity host. " +
                "See Documentation~/UNITY_ACTIVITY_OVERRIDE.md.");
        }
        FragmentManager fm = ((FragmentActivity) host).getSupportFragmentManager();
        AppUpdateResultFragment existing = (AppUpdateResultFragment) fm.findFragmentByTag(TAG);
        if (existing != null) return existing;

        AppUpdateResultFragment fragment = new AppUpdateResultFragment();
        try {
            fm.beginTransaction().add(fragment, TAG).commitNowAllowingStateLoss();
        } catch (IllegalStateException ise) {
            android.util.Log.e("BizSimAppUpdate",
                "Fragment attach failed (manager saved-state race): " + ise.getMessage());
            throw new IllegalStateException(
                "AppUpdateResultFragment attach failed. Fragment manager rejected the transaction " +
                "(usually an activity-recreation race). Retry after the activity is fully resumed.", ise);
        }
        return fragment;
    }

    @Override public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setRetainInstance(true);
    }

    public void setFlowLaunchCallback(AppUpdateBridge.IFlowLaunchCallback cb) {
        this.pendingCallback = cb;
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != AppUpdateBridge.REQ_FLEXIBLE &&
            requestCode != AppUpdateBridge.REQ_IMMEDIATE) return;

        int inAppCode = (resultCode == ActivityResult.RESULT_IN_APP_UPDATE_FAILED)
            ? ActivityResult.RESULT_IN_APP_UPDATE_FAILED : 0;

        if (pendingCallback != null) {
            pendingCallback.onActivityResult(resultCode, inAppCode);
            pendingCallback = null;
        }
    }
}
