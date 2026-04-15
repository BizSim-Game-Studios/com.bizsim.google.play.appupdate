package com.bizsim.google.play.appupdate;

import android.app.Activity;
import android.content.Context;
import android.os.Handler;
import android.os.Looper;

import com.google.android.play.core.appupdate.AppUpdateInfo;
import com.google.android.play.core.appupdate.AppUpdateManager;
import com.google.android.play.core.appupdate.AppUpdateManagerFactory;
import com.google.android.play.core.appupdate.AppUpdateOptions;
import com.google.android.play.core.install.InstallStateUpdatedListener;
import com.google.android.play.core.install.model.ActivityResult;
import com.google.android.play.core.install.model.AppUpdateType;
import com.google.android.play.core.install.model.UpdateAvailability;

import com.google.android.gms.tasks.Task;

public final class AppUpdateBridge {
    private static final String TAG = "BizSimAppUpdate";

    public static final int REQ_FLEXIBLE  = 0x42F1;
    public static final int REQ_IMMEDIATE = 0x42F2;

    // Callback interfaces — implemented in C# via AndroidJavaProxy.
    public interface IUpdateInfoCallback {
        void onInfoReceived(int updateAvailability, int availableVersionCode,
                            int updatePriority, int clientVersionStalenessDays, boolean stalenessPresent,
                            int installStatus, long bytesDownloaded, long totalBytesToDownload,
                            boolean flexibleAllowed, boolean immediateAllowed);
        void onInfoError(int errorCode, String message);
    }

    public interface IFlowLaunchCallback {
        void onLaunched();
        void onActivityResult(int resultCode, int inAppUpdateResultCode);
        void onLaunchError(int errorCode, String message);
    }

    public interface IInstallStateCallback {
        void onStateUpdate(int installStatus, long bytesDownloaded, long totalBytesToDownload, int installErrorCode);
    }

    public interface ICompleteCallback {
        void onCompleteInvoked();
        void onCompleteError(int errorCode, String message);
    }

    private static AppUpdateBridge sInstance;
    public static synchronized AppUpdateBridge getInstance() { return sInstance; }

    private final AppUpdateManager manager;
    private final Activity activity;
    private final Handler mainHandler;
    private final AppUpdateResultFragment resultFragment;
    private final InstallStateListenerBridge listenerBridge;

    public static synchronized AppUpdateBridge init(Activity activity) {
        if (sInstance == null) {
            sInstance = new AppUpdateBridge(activity);
        }
        return sInstance;
    }

    private AppUpdateBridge(Activity activity) {
        this.activity = activity;
        this.mainHandler = new Handler(Looper.getMainLooper());
        this.manager = AppUpdateManagerFactory.create(activity.getApplicationContext());
        this.resultFragment = AppUpdateResultFragment.attachTo(activity);
        this.listenerBridge = new InstallStateListenerBridge(manager);
    }

    public void setInstallStateCallback(IInstallStateCallback cb) {
        listenerBridge.setCallback(cb);
        listenerBridge.register();
    }

    public void unregisterInstallStateListener() {
        listenerBridge.unregister();
    }

    public void requestAppUpdateInfo(IUpdateInfoCallback cb) {
        mainHandler.post(() -> {
            try {
                Task<AppUpdateInfo> task = manager.getAppUpdateInfo();
                task.addOnCompleteListener(t -> {
                    if (t.isSuccessful()) {
                        AppUpdateInfo info = t.getResult();
                        Integer staleness = info.clientVersionStalenessDays();
                        cb.onInfoReceived(
                            info.updateAvailability(),
                            info.availableVersionCode(),
                            info.updatePriority(),
                            staleness == null ? 0 : staleness,
                            staleness != null,
                            info.installStatus(),
                            info.bytesDownloaded(),
                            info.totalBytesToDownload(),
                            info.isUpdateTypeAllowed(AppUpdateType.FLEXIBLE),
                            info.isUpdateTypeAllowed(AppUpdateType.IMMEDIATE)
                        );
                    } else {
                        cb.onInfoError(-100, t.getException() != null ? t.getException().getMessage() : "info failed");
                    }
                });
            } catch (Throwable thr) {
                cb.onInfoError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }

    public void startUpdateFlow(int appUpdateType, boolean allowAssetPackDeletion, IFlowLaunchCallback cb) {
        mainHandler.post(() -> {
            try {
                manager.getAppUpdateInfo().addOnCompleteListener(t -> {
                    if (!t.isSuccessful()) {
                        cb.onLaunchError(-100, t.getException() != null ? t.getException().getMessage() : "info failed");
                        return;
                    }
                    AppUpdateInfo info = t.getResult();
                    AppUpdateOptions opts = AppUpdateOptions
                        .newBuilder(appUpdateType)
                        .setAllowAssetPackDeletion(allowAssetPackDeletion)
                        .build();
                    int reqCode = (appUpdateType == AppUpdateType.IMMEDIATE) ? REQ_IMMEDIATE : REQ_FLEXIBLE;
                    resultFragment.setFlowLaunchCallback(cb);
                    try {
                        boolean ok = manager.startUpdateFlowForResult(info, activity, opts, reqCode);
                        if (ok) {
                            cb.onLaunched();
                        } else {
                            cb.onLaunchError(-5, "update type not allowed");
                        }
                    } catch (android.content.IntentSender.SendIntentException sise) {
                        cb.onLaunchError(-100, sise.getMessage() == null ? "" : sise.getMessage());
                    }
                });
            } catch (Throwable thr) {
                cb.onLaunchError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }

    public void completeUpdate(ICompleteCallback cb) {
        mainHandler.post(() -> {
            try {
                Task<Void> task = manager.completeUpdate();
                task.addOnSuccessListener(unused -> cb.onCompleteInvoked());
                task.addOnFailureListener(e ->
                    cb.onCompleteError(-100, e.getMessage() == null ? "" : e.getMessage()));
            } catch (Throwable thr) {
                cb.onCompleteError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }
}
