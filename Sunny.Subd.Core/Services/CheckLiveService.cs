using Sunny.Subd.Core.Facebook;

namespace Sunny.Subd.Core.Services
{
    /// <summary>
    /// Kiểm tra live/die cho danh sách UID Facebook.
    /// Chạy 10 luồng đồng thời, gọi callback mỗi khi có kết quả.
    /// </summary>
    public class CheckLiveService
    {
        private const int Concurrency = 10;

        /// <summary>
        /// Kiểm tra toàn bộ UIDs.
        /// </summary>
        /// <param name="uids">Danh sách UID cần kiểm tra (đã lọc trùng).</param>
        /// <param name="onProgress">
        ///   Callback mỗi khi 1 UID hoàn thành.
        ///   Tham số: (uid, isLive, doneCount, totalCount)
        /// </param>
        /// <param name="cancellationToken">Token huỷ khi cần dừng giữa chừng.</param>
        /// <returns>Dictionary[uid] = isLive</returns>
        public static async Task<Dictionary<string, bool>> RunAsync(
            IReadOnlyList<string> uids,
            Action<string, bool, int, int>? onProgress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (uids == null || uids.Count == 0) return result;

            int total = uids.Count;
            int done = 0;
            var lockObj = new object();

            using var semaphore = new SemaphoreSlim(Concurrency, Concurrency);

            var tasks = uids.Select(async uid =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    bool isLive = await FacebookRequest.CheckLive(uid).ConfigureAwait(false);

                    int current;
                    lock (lockObj)
                    {
                        result[uid] = isLive;
                        current = ++done;
                    }

                    onProgress?.Invoke(uid, isLive, current, total);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return result;
        }
    }
}
