using System;
using System.Linq;
using System.Threading.Tasks;
using CodeElements.BizRunner;
using CodeElements.BizRunner.Generic;
using Tasks.Infrastructure.Core;
using Tasks.Infrastructure.Management;
using Tasks.Infrastructure.Server.BusinessDataAccess;
using Tasks.Infrastructure.Server.Core;

namespace Tasks.Infrastructure.Server.Business.TaskManager
{
    public interface IEnableTaskAction : IGenericActionInOnlyAsync<Guid> { }

    public class EnableTaskAction : BusinessActionErrors, IEnableTaskAction
    {
        private readonly IMazeTaskManagerManagement _management;
        private readonly ITaskReferenceDbAccess _dbAccess;
        private readonly ITaskDirectory _taskDirectory;

        public EnableTaskAction(IMazeTaskManagerManagement management, ITaskReferenceDbAccess dbAccess, ITaskDirectory taskDirectory)
        {
            _management = management;
            _dbAccess = dbAccess;
            _taskDirectory = taskDirectory;
        }
        
        public async Task BizActionAsync(Guid inputData)
        {
            var tasks = await _taskDirectory.LoadTasks();
            var task = tasks.FirstOrDefault(x => x.Id == inputData);
            if (task == null)
            {
                AddValidationResult(TaskErrors.TaskNotFound);
                return;
            }

            await _dbAccess.SetTaskIsEnabled(inputData, true);
            var taskReference = await _dbAccess.FindAsync(inputData);
            var hash = _taskDirectory.ComputeTaskHash(task);

            await _management.InitializeTask(task, hash, true, !taskReference.IsCompleted);
        }
    }
}