using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace Snappass
{
	public sealed class SqliteStore : IMemoryStore, IDisposable
	{
		private class Secret 
		{
			public string Key { get; set; }
			public TimeToLive TimeToLive { get; set; }
			public string EncryptedPassword { get; set; }
			public DateTime StoredDateTime { get; set; }
		}
		private class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
		{
			private static readonly DateTimeHandler Default = new DateTimeHandler();
			internal const string FORMAT = "yyyy-MM-dd HH:mm";

			public override DateTime Parse(object value)
			{
				if (value == null)
				{
					return DateTime.MinValue;
				}
				var parsed = DateTime.ParseExact(value.ToString(), FORMAT, CultureInfo.InvariantCulture);
				return parsed;
			}

			public override void SetValue(IDbDataParameter parameter, DateTime value)
			{
				parameter.Value = value.ToString(FORMAT, CultureInfo.InvariantCulture);
			}
		}
		private class TimeToLiveHandler : SqlMapper.TypeHandler<TimeToLive>
		{
			public override TimeToLive Parse(object value)
			{
				int.TryParse(value.ToString(), out int intValue);
				return intValue switch
				{
					0 => TimeToLive.Hour,
					1 => TimeToLive.Day,
					2 => TimeToLive.Week,
					3 => TimeToLive.TwoWeeks,
					_ => TimeToLive.Hour,
				};
			}

			public override void SetValue(IDbDataParameter parameter, TimeToLive value)
			{
				parameter.Value = (int)value;
			}
		}
		private readonly SQLiteConnection _sqliteConnection;
		private readonly ILogger<SqliteStore> _logger;
		private readonly IDateTimeProvider _dateTimeProvider;
		private bool _disposed;

		public SqliteStore(SQLiteConnection sqliteConnection, ILogger<SqliteStore> logger, IDateTimeProvider dateTimeProvider)
		{
			_sqliteConnection = sqliteConnection;
			_logger = logger;
			_dateTimeProvider = dateTimeProvider;
			SqlMapper.AddTypeHandler(new TimeToLiveHandler());
			SqlMapper.AddTypeHandler(new DateTimeHandler());
		}
		public bool Has(string key)
		{
			var query = $@"
				SELECT EXISTS (
					SELECT 1 
					FROM SECRET
					WHERE Key = @key
				)
			";
			return _sqliteConnection.ExecuteScalar<bool>(query, new { Key = key });
		}

		public string Retrieve(string key)
		{
			if (key == null)
			{
				_logger.Log(LogLevel.Warning, $@"Tried to retrieve null key");
				return null;
			}
			if (!Has(key))
			{
				_logger.Log(LogLevel.Warning, $@"Tried to retrieve password for unknown key [{key}]");
				return null;
			}
			var query = $@"
				SELECT Key, TimeToLive, EncryptedPassword, StoredDateTime
				FROM SECRET
				WHERE Key = @key
			";
			var secret = _sqliteConnection.QuerySingle<Secret>(query, new { Key = key });
			static DateTime GetAtTheLatest(TimeToLive ttl, DateTime dateTime) => ttl switch
			{
				TimeToLive.Day => dateTime.AddDays(1),
				TimeToLive.Week => dateTime.AddDays(7),
				TimeToLive.Hour => dateTime.AddHours(1),
				TimeToLive.TwoWeeks => dateTime.AddDays(14),
				_ => dateTime.AddHours(1)
			};
			DateTime atTheLatest = GetAtTheLatest(secret.TimeToLive, secret.StoredDateTime);
			if (_dateTimeProvider.Now > atTheLatest)
			{
				static string ToString(TimeToLive ttl) => ttl switch
				{
					TimeToLive.Week => "1 week",
					TimeToLive.Day => "1 day",
					TimeToLive.Hour => "1 hour",
					TimeToLive.TwoWeeks => "2 weeks",
					_ => "hour"
				};
				var ttlString = ToString(secret.TimeToLive);
				_logger.Log(LogLevel.Warning, $@"Tried to retrieve password for key [{key}] after date is expired. Key set at [{secret.StoredDateTime}] for [{ttlString}]");
				Remove(key);
				return null;
			}
			Remove(key);
			return secret.EncryptedPassword;
		}

		private void Remove(string key)
		{
			var query = $@"
					DELETE
					FROM SECRET
					WHERE Key = @key
				";
			_sqliteConnection.Execute(query, new { Key = key });
		}

		public void Store(string encryptedPassword, string key, TimeToLive timeToLive)
		{
			var query = $@"
				INSERT INTO Secret (Key, TimeToLive, EncryptedPassword, StoredDateTime)
				VALUES (@key, @timeToLive, @encryptedPassword, @storedDateTime)
			";
			var storedDateTime = DateTime.Now.ToString(DateTimeHandler.FORMAT);
			var parameters = new
			{
				Key = key,
				TimeToLive = timeToLive,
				EncryptedPassword = encryptedPassword,
				StoredDateTime = storedDateTime
			};
			_sqliteConnection.Execute(query, parameters);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_sqliteConnection.Dispose();
				_disposed = true;
			}
		}
	}
}