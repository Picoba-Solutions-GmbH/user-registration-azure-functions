using Azure.Data.Tables;
using System;

namespace SimpleUserManagement.Implementations
{
    public class TableServiceClientWrapper
    {
        private TableClient tableClient;

        public TableClient GetTableClient()
        {
            if (this.tableClient != null) return this.tableClient;

            var serviceClient = new TableServiceClient(
                new Uri("https://myAzureTable.table.core.windows.net"),
                new TableSharedKeyCredential(Environment.GetEnvironmentVariable("TableStorageAccountName"), Environment.GetEnvironmentVariable("TableStorageAccountKey")));
            this.tableClient = serviceClient.GetTableClient(Environment.GetEnvironmentVariable("TableStorageTableName"));
            return this.tableClient;
        }
    }
}
