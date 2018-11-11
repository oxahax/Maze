﻿using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Options;
using Tasks.Infrastructure.Management.Data;
using Tasks.Infrastructure.Server.BusinessDataAccess.Base;
using Tasks.Infrastructure.Server.Options;

namespace Tasks.Infrastructure.Server.BusinessDataAccess
{
    public interface ITaskTransmissionsDbAccess
    {
        Task CreateAsync(TaskTransmission taskTransmission);
    }

    public class TaskTransmissionsDbAccess : SqliteDbAccess, ITaskTransmissionsDbAccess
    {
        public TaskTransmissionsDbAccess(IOptions<TasksOptions> options) : base(options)
        {
        }

        public async Task CreateAsync(TaskTransmission taskTransmission)
        {
            using (var connection = await OpenConnection())
            {
                await connection.InsertAsync(taskTransmission);
            }
        }
    }
}