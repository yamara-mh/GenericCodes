using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 最後に実行した UniTask のみ処理を継続し、過去に実行した UniTask をキャンセル・破棄するクラス
/// </summary>
public class LatestTaskRunner<HANDLE> : IDisposable
{
    private Action<HANDLE> _releaseAction;
    private CancellationToken _lifespanCt;
    private CancellationTokenSource _currentCts;

    private void TryRelease(HANDLE handle)
    {
        if (_releaseAction == null) return;
        if (Equals(handle, default(HANDLE))) return;
        _releaseAction(handle);
    }

    /// <summary>
    /// キャンセル時など、不要になった際にリソースを解放する処理を登録
    /// </summary>
    public LatestTaskRunner<HANDLE> WithRelease(Action<HANDLE> releaseAction)
    {
        _releaseAction = releaseAction;
        return this;
    }

    /// <summary>
    /// コンポーネントの寿命に紐づけ、Destroy時に自動キャンセル
    /// </summary>
    public LatestTaskRunner<HANDLE> BindTo(Component lifespan)
    {
        if (lifespan != null) _lifespanCt = lifespan.GetCancellationTokenOnDestroy();
        return this;
    }

    /// <summary>
    /// 非同期処理を実行し、直前の処理が実行中であればキャンセル
    /// </summary>
    public async UniTask<(bool isCanceled, HANDLE handle)> Run(Func<CancellationToken, UniTask<HANDLE>> taskFunc)
    {
        Cancel(); // 前回実行中のタスクがあればキャンセルする

        // BindToで紐付けたCancellationTokenとリンクした新しいCancellationTokenSourceを作成
        var currentCts = CancellationTokenSource.CreateLinkedTokenSource(_lifespanCt);
        _currentCts = currentCts;
        var token = currentCts.Token;

        HANDLE result = default;
        try
        {
            result = await taskFunc(token); // タスクを実行

            // 実行終了後にキャンセル状態かチェック（await 中に次の Run が呼ばれた場合など）
            if (token.IsCancellationRequested)
            {
                // すでに結果が生成されてしまった場合は、登録したReleaseアクションで解放する
                TryRelease(result);
                return (true, default);
            }

            return (false, result); // 正常完了
        }
        catch (OperationCanceledException)
        {
            return (true, default); // taskFunc 内でキャンセル例外が投げられた場合
        }
        catch (Exception e)
        {
            Debug.LogError($"[LatestTaskRunner] {e}"); // 想定外の例外
            return (true, default);
        }
        finally
        {
            if (_currentCts == currentCts) _currentCts = null; // 自分が最新実行ならアクティブ参照を外す
            currentCts.Dispose(); // この実行で生成したCTSを確実に解放する
        }
    }

    /// <summary>
    /// 現在実行中の処理を手動でキャンセル
    /// </summary>
    public void Cancel()
    {
        if (_currentCts != null)
        {
            _currentCts.Cancel();
            _currentCts.Dispose();
            _currentCts = null;
        }
    }

    public void Dispose()
    {
        Cancel();
        _releaseAction = null;
    }
}
