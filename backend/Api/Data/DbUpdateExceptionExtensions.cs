using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Api.Data;

public static class DbUpdateExceptionExtensions
{
    // True when MySQL refused the write because of a unique index (error 1062). Controllers check
    // uniqueness first to give a clear message; this catches the rare case of two simultaneous requests.
    public static bool IsDuplicateKey(this DbUpdateException exception) =>
        exception.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };
}
