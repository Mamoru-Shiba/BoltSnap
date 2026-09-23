using BoltSnap.Core.Services;
using BoltSnap.Services;
using BoltSnap.Ui;

// タスクバーの空きの測定だけを行う別プロセスとして起動されたとき（本体のメモリを増やさないため）
if (args is [TaskbarProbeService.ProbeArgument, var taskbarHandle, var trayLeft])
{
    return TaskbarProbeService.RunProbe(taskbarHandle, trayLeft);
}

// 二重起動しない
using var mutex = new Mutex(initiallyOwned: true, "Local\\BoltSnap.SingleInstance", out var isFirst);
if (!isFirst)
{
    return 0;
}

new BarWindow(new ActivityStoreService(ActivityStoreService.DefaultDirectory)).Run();
return 0;
