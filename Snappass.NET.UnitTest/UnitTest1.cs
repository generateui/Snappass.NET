using Dapper;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data.SQLite;
using Xunit;

namespace Snappass.NET.UnitTest
{
	public class UnitTest1
	{
		private static SQLiteConnection CreateConnection()
		{
			var sqliteConnection = new SQLiteConnection("Data Source=:memory:");
			var createScript = $@"
				CREATE TABLE ""Secret"" (
					""Key""	TEXT UNIQUE,
					""TimeToLive""	INTEGER NOT NULL,
					""EncryptedPassword""	TEXT NOT NULL,
					""StoredDateTime""	TEXT NOT NULL,
					PRIMARY KEY(""Key"")
				)
			";
			sqliteConnection.Open();
			sqliteConnection.Execute(createScript);
			return sqliteConnection;
		}

		[Fact]
		public void StoredKey_IsStored()
		{
			// Arrange
			var sqliteConnection = CreateConnection();
			var sqliteStore = new SqliteStore(sqliteConnection, Mock.Of<ILogger<SqliteStore>>(), new CurrentDateTimeProvider());
			sqliteStore.Store("encrypted", "key", TimeToLive.Day);

			// Act
			var wasStored = sqliteStore.Has("key");

			// Assert
			Assert.True(wasStored);
		}

		[Fact]
		public void StoredKey_CanBeRetrieved()
		{
			// Arrange
			var sqliteConnection = CreateConnection();
			var sqliteStore = new SqliteStore(sqliteConnection, Mock.Of<ILogger<SqliteStore>>(), new CurrentDateTimeProvider());
			sqliteStore.Store("encrypted", "key", TimeToLive.Day);

			// Act
			var hasAfterStoring = sqliteStore.Has("key");
			string result = sqliteStore.Retrieve("key");

			// Assert
			Assert.Equal("encrypted", result);
		}

		[Fact]
		public void StoredKeyExpired_DoesNotRetrieveSecret()
		{
			// Arrange
			var sqliteConnection = CreateConnection();
			var dateTimeProviderMock = new Mock<IDateTimeProvider>();
			var inTheFuture = DateTime.Now.AddHours(2);
			dateTimeProviderMock.Setup(x => x.Now).Returns(inTheFuture);
			var sqliteStore = new SqliteStore(sqliteConnection, Mock.Of<ILogger<SqliteStore>>(), dateTimeProviderMock.Object);
			sqliteStore.Store("encrypted", "key", TimeToLive.Hour);

			// Act
			bool wasStored = sqliteStore.Has("key");
			string result = sqliteStore.Retrieve("key");
			bool wasStoredAfterTryRetrieve = sqliteStore.Has("key");

			// Assert
			Assert.Null(result);
			Assert.False(wasStoredAfterTryRetrieve);
		}
	}
}
