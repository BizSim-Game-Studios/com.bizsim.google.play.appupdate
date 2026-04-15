package com.bizsim.google.play.appupdate;

import com.google.android.play.core.appupdate.AppUpdateManager;
import com.google.android.play.core.install.InstallState;
import com.google.android.play.core.install.InstallStateUpdatedListener;

public final class InstallStateListenerBridge {
    private final AppUpdateManager manager;
    private AppUpdateBridge.IInstallStateCallback callback;
    private InstallStateUpdatedListener listener;
    private boolean registered;

    public InstallStateListenerBridge(AppUpdateManager manager) {
        this.manager = manager;
    }

    public void setCallback(AppUpdateBridge.IInstallStateCallback cb) {
        this.callback = cb;
    }

    public synchronized void register() {
        if (registered) return;
        listener = state -> {
            if (callback == null) return;
            callback.onStateUpdate(
                state.installStatus(),
                state.bytesDownloaded(),
                state.totalBytesToDownload(),
                state.installErrorCode());
        };
        manager.registerListener(listener);
        registered = true;
    }

    public synchronized void unregister() {
        if (!registered || listener == null) return;
        manager.unregisterListener(listener);
        listener = null;
        registered = false;
    }
}
