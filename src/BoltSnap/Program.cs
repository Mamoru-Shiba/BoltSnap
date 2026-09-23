using BoltSnap.Core.Services;
using BoltSnap.Ui;

// 二重起動しない
using var mutex = new Mutex(initiallyOwned: true, "Local\\BoltSnap.SingleInstance", out var isFirst);
if (!isFirst)
{
    return;
}

new BarWindow(new ActivityStoreService(ActivityStoreService.DefaultDirectory)).Run();
