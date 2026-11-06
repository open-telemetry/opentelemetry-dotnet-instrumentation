// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using NSubstitute;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;
using OpenTelemetry.Instrumentation;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.AdoNet;

[Collection("Non-Parallel Collection")]
public class AdoNetSqlDialectTests
{
    [Theory]
    [InlineData("Devart.Data.Oracle.OracleCommand", true, "oracle.db", (int)SqlProcessor.SqlDialect.Oracle)]
    [InlineData("Devart.Data.MySql.MySqlCommand", true, "mysql", (int)SqlProcessor.SqlDialect.MySql)]
    [InlineData("Devart.Data.PostgreSql.PgSqlCommand", true, "postgresql", (int)SqlProcessor.SqlDialect.Default)]
    [InlineData("Microsoft.Data.SqlClient.SqlCommand", false, "microsoft.sql_server", (int)SqlProcessor.SqlDialect.SqlServer)]
    [InlineData("MySqlConnector.MySqlCommand", false, "mysql", (int)SqlProcessor.SqlDialect.MySql)]
    [InlineData("MySql.Data.MySqlClient.MySqlCommand", false, "mysql", (int)SqlProcessor.SqlDialect.MySql)]
    [InlineData("Npgsql.NpgsqlCommand", false, "postgresql", (int)SqlProcessor.SqlDialect.Default)]
    [InlineData("Oracle.ManagedDataAccess.Client.OracleCommand", false, "oracle.db", (int)SqlProcessor.SqlDialect.Oracle)]
    [InlineData("System.Data.SqlClient.SqlCommand", false, "microsoft.sql_server", (int)SqlProcessor.SqlDialect.SqlServer)]
    [InlineData("System.Data.SQLite.SQLiteCommand", false, "sqlite", (int)SqlProcessor.SqlDialect.Sqlite)]
    [InlineData("Unknown.Provider.Command", true, "other_sql", (int)SqlProcessor.SqlDialect.Default)]
    public void SelectsInstrumentationDetails(string commandTypeName, bool enabled, string systemName, int expectedDialect)
    {
        var commandType = CreateCommandType(commandTypeName);

        var details = AdoNetDbSystemMapper.GetInstrumentationDetails(commandType);

        Assert.Equal(enabled, details.Enabled);
        Assert.Equal(systemName, details.SystemName);
        Assert.Equal((SqlProcessor.SqlDialect)expectedDialect, details.Dialect);
        Assert.Equal(details, AdoNetDbSystemMapper.GetInstrumentationDetails(commandType));
    }

    [Fact]
    public void SelectsSqliteInstrumentationDetails()
    {
        var details = AdoNetDbSystemMapper.GetInstrumentationDetails(CreateCommandType("Microsoft.Data.Sqlite.SqliteCommand"));

        Assert.Equal(Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(TracerInstrumentation.Sqlite), details.Enabled);
        Assert.Equal("sqlite", details.SystemName);
        Assert.Equal(SqlProcessor.SqlDialect.Sqlite, details.Dialect);
    }

    [Theory]
    [InlineData("Npgsql.NpgsqlCommand")]
    [InlineData("Devart.Data.MySql.MySqlCommand")]
    [InlineData("Devart.Data.Oracle.OracleCommand")]
    [InlineData("Microsoft.Data.Sqlite.SqliteCommand")]
    [InlineData("Unknown.Provider.Command")]
    public void InheritsInstrumentationDetailsFromBaseCommand(string commandTypeName)
    {
        var commandType = CreateCommandType(commandTypeName);
        var derivedType = CreateCommandType("Derived.Provider.Command", commandType);
        var furtherDerivedType = CreateCommandType("FurtherDerived.Provider.Command", derivedType);
        var expected = AdoNetDbSystemMapper.GetInstrumentationDetails(commandType);

        Assert.Equal(expected, AdoNetDbSystemMapper.GetInstrumentationDetails(derivedType));
        Assert.Equal(expected, AdoNetDbSystemMapper.GetInstrumentationDetails(furtherDerivedType));
    }

    [Theory]
    [InlineData("Devart.Data.MySql.MySqlCommand", "SELECT \"topsecret\"", "SELECT ?")]
    [InlineData("Devart.Data.Oracle.OracleCommand", "SELECT q'[topsecret]' FROM dual", "SELECT ? FROM dual")]
    [InlineData("Microsoft.Data.Sqlite.SqliteCommand", "SELECT ['Tis], 'topsecret'", "SELECT ['Tis], ?")]
    [InlineData("Devart.Data.PostgreSql.PgSqlCommand", "SELECT E'topsecret'", "SELECT ?")]
    [InlineData(
        "Devart.Data.PostgreSql.PgSqlCommand",
        "SELECT * FROM t WHERE a IN ('x', b$$tag$) AND d = '$tag$) secret-name'",
        "SELECT * FROM t WHERE a IN (?) AND d = ?")]
    [InlineData("Unknown.Provider.Command", "SELECT $$topsecret$$", "SELECT ?")]
    public void SanitizesUsingSelectedDialect(string commandTypeName, string sql, string expected)
    {
        var details = AdoNetDbSystemMapper.GetInstrumentationDetails(CreateCommandType(commandTypeName));
        var statement = SqlProcessor.GetSanitizedSql(sql, details.Dialect);
        Assert.Equal(expected, statement.SanitizedSql);
        Assert.DoesNotContain("topsecret", statement.DbQuerySummary, StringComparison.Ordinal);
    }

    [Fact]
    public void KeepsCachesSeparateAcrossDialects()
    {
        const string sql = "SELECT \"topsecret\"";
        var postgresql = AdoNetDbSystemMapper.GetInstrumentationDetails(CreateCommandType("Devart.Data.PostgreSql.PgSqlCommand")).Dialect;
        var mysql = AdoNetDbSystemMapper.GetInstrumentationDetails(CreateCommandType("Devart.Data.MySql.MySqlCommand")).Dialect;

        Assert.Equal(sql, SqlProcessor.GetSanitizedSql(sql, postgresql).SanitizedSql);
        Assert.Equal("SELECT ?", SqlProcessor.GetSanitizedSql(sql, mysql).SanitizedSql);
        Assert.Equal(sql, SqlProcessor.GetSanitizedSql(sql, postgresql).SanitizedSql);
    }

    private static Type CreateCommandType(string name, Type? baseType = null)
    {
        var commandType = Substitute.For<Type>();
        commandType.UnderlyingSystemType.Returns(commandType);
        commandType.FullName.Returns(name);
        commandType.BaseType.Returns(baseType);
        return commandType;
    }
}
