﻿using ETLBox.Connection;
using ETLBox.Helper;
using System;


namespace ETLBox.ControlFlow.Tasks
{
    /// <summary>
    /// Count the row in a table. This task normally uses the  COUNT(*) method (could take some time on big tables).
    /// You can pass a a filter condition for the count.
    /// </summary>
    /// <example>
    /// <code>
    /// int count = RowCountTask.Count("tableName").Value;
    /// </code>
    /// </example>
    public sealed class RowCountTask : ControlFlowTask
    {
        /// <inheritdoc/>
        public override string TaskName { get; set; } = $"Count Rows for table";

        /// <summary>
        /// Name of the table on which the rows are counted
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The formatted table table name
        /// </summary>
        public ObjectNameDescriptor TN => new ObjectNameDescriptor(TableName, QB, QE);

        /// <summary>
        /// Part of the sql where condition which restrict which rows are counted
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Will hold the number of counted rows after execution
        /// </summary>
        public int? Rows { get; private set; }

        /// <summary>
        /// Indicates if the table contains rows - only has a value after the execution
        /// </summary>
        public bool? HasRows => Rows > 0;

        /// <summary>
        /// For Sql Server, you can set the QuickQueryMode to true. This will query the sys.partition table which can be much faster.
        /// </summary>
        public bool QuickQueryMode { get; set; }

        /// <summary>
        /// NoLock does a normal COUNT(*) using the nolock - option which avoid tables locks when reading from the table
        /// (but while counting the tables new data could be inserted, which could lead to wrong results).
        /// </summary>
        public bool NoLock { get; set; }

        /// <summary>
        /// The sql that is executed to count the rows in the table - will change depending on your parameters.
        /// </summary>
        public string Sql
        {
            get
            {
                return QuickQueryMode && !HasCondition ? $@"
SELECT SUM ([rows]) 
FROM [sys].[partitions] 
WHERE [object_id] = object_id(N'{TableName}') 
  AND [index_id] IN (0,1)" :
                $@"
SELECT COUNT(*)
FROM {TN.QuotatedFullName} 
{WhereClause} {Condition} {NoLockHint}";
            }
        }

        /// <summary>
        /// Performs the row count
        /// </summary>
        public void Execute()
        {
            Rows = new SqlTask(this, Sql).ExecuteScalar<int>();
        }

        public RowCountTask() { }

        public RowCountTask(string tableName)
        {
            this.TableName = tableName;
        }

        public RowCountTask(string tableName, RowCountOptions options) : this(tableName)
        {
            if (options == RowCountOptions.QuickQueryMode)
                QuickQueryMode = true;
            if (options == RowCountOptions.NoLock)
                NoLock = true;

        }

        public RowCountTask(string tableName, string condition) : this(tableName)
        {
            this.Condition = condition;
        }


        public RowCountTask(string tableName, string condition, RowCountOptions options) : this(tableName, options)
        {
            this.Condition = condition;
        }

        public RowCountTask Count()
        {
            Execute();
            return this;
        }

        public static int? Count(string tableName) => new RowCountTask(tableName).Count().Rows;
        public static int? Count(string tableName, RowCountOptions options) => new RowCountTask(tableName, options).Count().Rows;
        public static int? Count(string tableName, string condition) => new RowCountTask(tableName, condition).Count().Rows;
        public static int? Count(string tableName, string condition, RowCountOptions options) => new RowCountTask(tableName, condition, options).Count().Rows;
        public static int? Count(IConnectionManager connectionManager, string tableName) => new RowCountTask(tableName) { ConnectionManager = connectionManager }.Count().Rows;
        public static int? Count(IConnectionManager connectionManager, string tableName, RowCountOptions options) => new RowCountTask(tableName, options) { ConnectionManager = connectionManager }.Count().Rows;
        public static int? Count(IConnectionManager connectionManager, string tableName, string condition) => new RowCountTask(tableName, condition) { ConnectionManager = connectionManager }.Count().Rows;
        public static int? Count(IConnectionManager connectionManager, string tableName, string condition, RowCountOptions options) => new RowCountTask(tableName, condition, options) { ConnectionManager = connectionManager }.Count().Rows;

        bool HasCondition => !String.IsNullOrWhiteSpace(Condition);
        string WhereClause => HasCondition ? "WHERE" : String.Empty;
        string NoLockHint => NoLock ? "WITH (NOLOCK)" : String.Empty;

    }

    /// <summary>
    /// Used in the RowCountTask. None forces the RowCountTask to do a normal COUNT(*) and works on all databases.
    /// QuickQueryMode only works on SqlServer and uses the partition table which can be much faster on tables with a big amount of data.
    /// NoLock does a normal COUNT(*) using the nolock - option which avoid tables locks when reading from the table (but while counting the tables
    /// new data could be inserted, which could lead to wrong results).
    /// </summary>
    public enum RowCountOptions
    {
        None,
        QuickQueryMode,
        NoLock
    }
}
