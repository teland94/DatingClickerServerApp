using DatingClickerServerApp.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DatingClickerServerApp.UI.Pages
{
    public partial class Index : ComponentBase, IDisposable
    {
        private bool _loading;
        private int _repeatCount = 5;
        private string _datingResult;
        private CancellationTokenSource _cancellationTokenSource;

        [Inject] private DatingClickerProcessor DatingClickerProcessor { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        public void Dispose()
        {
            CancelOperation();
        }

        private async Task Run(bool onlineOnly)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            await InvokeAsync(StateHasChanged);

            try
            {
                _loading = true;
                _datingResult = string.Empty;

                DatingClickerProcessor.OnResultUpdated += UpdateResult;
                await DatingClickerProcessor.ProcessDatingUsers(onlineOnly, _repeatCount, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _datingResult = ex.Message;
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task UpdateResult(string result)
        {
            _datingResult += HighlightResult(result) + "<br>";

            await InvokeAsync(StateHasChanged);

            await ScrollToBottom();
        }

        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private ValueTask ScrollToBottom()
        {
            return JSRuntime.InvokeVoidAsync("scrollToBottom", "datingResultContainer");
        }

        private static string HighlightResult(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                return result;
            }

            var lines = result.Split("<br>");
            var highlightedLines = lines.Select(line =>
            {
                if (line.Contains("Dislike:"))
                {
                    return $"<span class=\"text-danger\">{line}</span>";
                }
                else if (line.Contains("Super Like:"))
                {
                    return $"<span class=\"text-success\">{line}</span>";
                }
                return line;
            });

            return string.Join("<br>", highlightedLines);
        }
    }
}
