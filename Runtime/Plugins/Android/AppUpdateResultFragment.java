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
        // Async commit (not commitNow): commitNow runs synchronously and throws
        // IllegalStateException when the FragmentManager is mid-transition during activity
        // recreation on cold start. commitAllowingStateLoss enqueues the add and runs it once
        // the manager is ready, dodging that race. Keep it NON-FATAL: this fragment only routes
        // the update-flow result — the info check, install-state listener and completeUpdate
        // paths do not need it, so bridge init must still succeed if the attach can't happen.
        try {
            fm.beginTransaction().add(fragment, TAG).commitAllowingStateLoss();
        } catch (IllegalStateException ise) {
            android.util.Log.w("BizSimAppUpdate",
                "AppUpdateResultFragment attach skipped (FragmentManager unavailable): " + ise.getMessage());
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

        final AppUpdateBridge.IFlowLaunchCallback cb = pendingCallback;
        if (cb != null) {
            pendingCallback = null;
            AppUpdateBridge.CALLBACK_EXECUTOR.execute(() -> cb.onActivityResult(resultCode, inAppCode));
        }
    }
}
