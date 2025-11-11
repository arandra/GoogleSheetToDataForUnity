using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
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

            return await request.ExecuteAsync();
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
    }
}
