using Db4Net.Commands;
using Db4Net.Query;

namespace Db4Net;

/// <summary>
/// Extension methods for building Db4Net statements inside a Db4Net-owned transaction.
/// </summary>
public static class Db4NetTransactionExtensions
{
    /// <inheritdoc cref="Db4NetDatabase.Select(string[])" />
    public static SelectQueryBuilder Select(this Db4NetTransaction transaction, params string[] columns)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Select(columns);
    }

    /// <inheritdoc cref="Db4NetDatabase.Select(IEnumerable{string})" />
    public static SelectQueryBuilder Select(this Db4NetTransaction transaction, IEnumerable<string> columns)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Select(columns);
    }

    /// <inheritdoc cref="Db4NetDatabase.Select{T}(System.Linq.Expressions.Expression{System.Func{T, object?}}[])" />
    public static SelectQueryBuilder<T> Select<T>(this Db4NetTransaction transaction, params System.Linq.Expressions.Expression<Func<T, object?>>[] memberSelectors)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Select(memberSelectors);
    }

    /// <inheritdoc cref="Db4NetDatabase.SelectFrom{T}()" />
    public static SelectQueryBuilder<T> SelectFrom<T>(this Db4NetTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.SelectFrom<T>();
    }

    /// <inheritdoc cref="Db4NetDatabase.SelectFrom{T}(string)" />
    public static SelectQueryBuilder<T> SelectFrom<T>(this Db4NetTransaction transaction, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.SelectFrom<T>(table);
    }

    /// <inheritdoc cref="Db4NetDatabase.SelectCountFrom{T}()" />
    public static SelectCountQueryBuilder<T> SelectCountFrom<T>(this Db4NetTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.SelectCountFrom<T>();
    }

    /// <inheritdoc cref="Db4NetDatabase.SelectCountFrom{T}(string)" />
    public static SelectCountQueryBuilder<T> SelectCountFrom<T>(this Db4NetTransaction transaction, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.SelectCountFrom<T>(table);
    }

    /// <inheritdoc cref="Db4NetDatabase.SelectExistsFrom{T}()" />
    public static SelectExistsQueryBuilder<T> SelectExistsFrom<T>(this Db4NetTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.SelectExistsFrom<T>();
    }

    /// <inheritdoc cref="Db4NetDatabase.SelectExistsFrom{T}(string)" />
    public static SelectExistsQueryBuilder<T> SelectExistsFrom<T>(this Db4NetTransaction transaction, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.SelectExistsFrom<T>(table);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertInto{T}()" />
    public static InsertCommandBuilder<T> InsertInto<T>(this Db4NetTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertInto<T>();
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertInto{T}(string)" />
    public static InsertCommandBuilder<T> InsertInto<T>(this Db4NetTransaction transaction, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertInto<T>(table);
    }

    /// <inheritdoc cref="Db4NetDatabase.Insert{T}(T)" />
    public static InsertCommandBuilder<T> Insert<T>(this Db4NetTransaction transaction, T entity)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Insert(entity);
    }

    /// <inheritdoc cref="Db4NetDatabase.Insert{T}(T, string)" />
    public static InsertCommandBuilder<T> Insert<T>(this Db4NetTransaction transaction, T entity, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Insert(entity, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrIgnore{T}(T)" />
    public static InsertOrIgnoreCommandBuilder<T> InsertOrIgnore<T>(this Db4NetTransaction transaction, T entity)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrIgnore(entity);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrIgnore{T}(T, string)" />
    public static InsertOrIgnoreCommandBuilder<T> InsertOrIgnore<T>(this Db4NetTransaction transaction, T entity, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrIgnore(entity, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrUpdate{T}(T)" />
    public static InsertOrUpdateCommandBuilder<T> InsertOrUpdate<T>(this Db4NetTransaction transaction, T entity)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrUpdate(entity);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrUpdate{T}(T, string)" />
    public static InsertOrUpdateCommandBuilder<T> InsertOrUpdate<T>(this Db4NetTransaction transaction, T entity, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrUpdate(entity, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertMany{T}(IEnumerable{T})" />
    public static InsertManyCommandBuilder<T> InsertMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertMany(entities);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertMany{T}(IEnumerable{T}, string)" />
    public static InsertManyCommandBuilder<T> InsertMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertMany(entities, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrIgnoreMany{T}(IEnumerable{T})" />
    public static InsertOrIgnoreManyCommandBuilder<T> InsertOrIgnoreMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrIgnoreMany(entities);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrIgnoreMany{T}(IEnumerable{T}, string)" />
    public static InsertOrIgnoreManyCommandBuilder<T> InsertOrIgnoreMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrIgnoreMany(entities, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrUpdateMany{T}(IEnumerable{T})" />
    public static InsertOrUpdateManyCommandBuilder<T> InsertOrUpdateMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrUpdateMany(entities);
    }

    /// <inheritdoc cref="Db4NetDatabase.InsertOrUpdateMany{T}(IEnumerable{T}, string)" />
    public static InsertOrUpdateManyCommandBuilder<T> InsertOrUpdateMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.InsertOrUpdateMany(entities, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.Update{T}()" />
    public static UpdateCommandBuilder<T> Update<T>(this Db4NetTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Update<T>();
    }

    /// <inheritdoc cref="Db4NetDatabase.Update{T}(string)" />
    public static UpdateCommandBuilder<T> Update<T>(this Db4NetTransaction transaction, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Update<T>(table);
    }

    /// <inheritdoc cref="Db4NetDatabase.Update{T}(T)" />
    public static UpdateCommandBuilder<T> Update<T>(this Db4NetTransaction transaction, T entity)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Update(entity);
    }

    /// <inheritdoc cref="Db4NetDatabase.Update{T}(T, string)" />
    public static UpdateCommandBuilder<T> Update<T>(this Db4NetTransaction transaction, T entity, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Update(entity, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.UpdateMany{T}(IEnumerable{T})" />
    public static UpdateManyCommandBuilder<T> UpdateMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.UpdateMany(entities);
    }

    /// <inheritdoc cref="Db4NetDatabase.UpdateMany{T}(IEnumerable{T}, string)" />
    public static UpdateManyCommandBuilder<T> UpdateMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.UpdateMany(entities, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.DeleteFrom{T}()" />
    public static DeleteCommandBuilder<T> DeleteFrom<T>(this Db4NetTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.DeleteFrom<T>();
    }

    /// <inheritdoc cref="Db4NetDatabase.DeleteFrom{T}(string)" />
    public static DeleteCommandBuilder<T> DeleteFrom<T>(this Db4NetTransaction transaction, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.DeleteFrom<T>(table);
    }

    /// <inheritdoc cref="Db4NetDatabase.Delete{T}(T)" />
    public static DeleteCommandBuilder<T> Delete<T>(this Db4NetTransaction transaction, T entity)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Delete(entity);
    }

    /// <inheritdoc cref="Db4NetDatabase.Delete{T}(T, string)" />
    public static DeleteCommandBuilder<T> Delete<T>(this Db4NetTransaction transaction, T entity, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.Delete(entity, table);
    }

    /// <inheritdoc cref="Db4NetDatabase.DeleteMany{T}(IEnumerable{T})" />
    public static DeleteManyCommandBuilder<T> DeleteMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.DeleteMany(entities);
    }

    /// <inheritdoc cref="Db4NetDatabase.DeleteMany{T}(IEnumerable{T}, string)" />
    public static DeleteManyCommandBuilder<T> DeleteMany<T>(this Db4NetTransaction transaction, IEnumerable<T> entities, string table)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return transaction.Database.DeleteMany(entities, table);
    }
}
