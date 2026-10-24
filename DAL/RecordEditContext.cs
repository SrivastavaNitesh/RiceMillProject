using Microsoft.Data.SqlClient;
using System.Data;

namespace RiceMillProject.DAL;

public static class RecordEditContext
{
    private static readonly AsyncLocal<int?> Actor = new();
    public static int? UserId { get => Actor.Value; set => Actor.Value = value; }
    public static void Open(SqlConnection connection)
    {
        connection.Open();
        using var command = new SqlCommand(@"EXEC sys.sp_set_session_context @key=N'RecordActorUserId',@value=@UserId;
            EXEC sys.sp_set_session_context @key=N'RecordEditPolicyId',@value=NULL;
            EXEC sys.sp_set_session_context @key=N'RecordEditReason',@value=NULL;", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = (object?)UserId ?? DBNull.Value;
        command.ExecuteNonQuery();
    }
}
