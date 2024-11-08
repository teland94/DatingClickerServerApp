using DatingClickerServerApp.Common;
using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Persistence;
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

        [Inject] private IDatingClickerService DatingClickerService { get; set; }
        [Inject] private IConfiguration Configuration { get; set; }
        [Inject] private AppDbContext DbContext { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IEncryptionService EncryptionService { get; set; }


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

                var settings = new DatingClickerProcessorSettings
                {
                    SignIn = Configuration.GetRequiredSection(nameof(DatingClickerProcessorSettings.SignIn)).GetChildren().ToDictionary(s => s.Key, s => s.Value),
                    LikeCriteries = Configuration.GetSection(nameof(DatingClickerProcessorSettings.LikeCriteries)).Get<DatingUserCriteriesSettings>()
                     ?? new DatingUserCriteriesSettings(155, [], ["plus size", "plus-size", "мужчину для кое чего интересного", "показать себя", "покажу себя"], null, false)
                };

                var processor = new DatingClickerProcessor(DatingClickerService, settings, DbContext, EncryptionService);
                processor.OnResultUpdated += UpdateResult;
                await processor.ProcessDatingUsers(onlineOnly, _repeatCount, _cancellationTokenSource.Token);
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
