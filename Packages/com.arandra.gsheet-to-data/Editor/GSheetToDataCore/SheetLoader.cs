using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace GSheetToDataCore
{
    public class SheetLoader
    {
        private static readonly string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };

        public async Task<ValueRange> LoadSheetAsync(
            string spreadsheetId,
            string sheetName,
            string clientSecretPath,
            string? tokenStorePath = null)
        {
            var credential = await GetUserCredentialAsync(clientSecretPath, tokenStorePath);
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleSheetToData",
            });

            var range = $"'{sheetName}'!A1:Z";
            var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            try
            {
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException ex) when (IsSheetRangeError(ex))
            {
                throw new InvalidOperationException(
                    BuildSheetNameErrorMessage(sheetName, range, ex),
                    ex);
            }
        }

        private async Task<UserCredential> GetUserCredentialAsync(string clientSecretPath, string? tokenStorePath)
        {
            var resolvedTokenStorePath = tokenStorePath;
            if (string.IsNullOrWhiteSpace(resolvedTokenStorePath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                resolvedTokenStorePath = Path.Combine(appData, "GoogleSheetToData", "TokenStore");
            }

            Directory.CreateDirectory(resolvedTokenStorePath);

            await using var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read);
            var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

            var dataStore = new FileDataStore(resolvedTokenStorePath, true);

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                "user",
                CancellationToken.None,
                dataStore);
        }

        private static bool IsSheetRangeError(GoogleApiException ex)
        {
            if (ex == null)
            {
                return false;
            }

            var message = ex.Message ?? string.Empty;
            return message.IndexOf("Unable to parse range", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildSheetNameErrorMessage(string sheetName, string range, GoogleApiException ex)
        {
            var apiMessage = ex.Message ?? "Unknown Google Sheets API error.";
            return
                $"Could not read sheet '{sheetName}'. " +
                $"Check that the Asset Manager 'Sheet Name' exactly matches the tab name in Google Sheets. " +
                $"Requested range: {range}. " +
                $"API message: {apiMessage}";
        }
    }
}
