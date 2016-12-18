using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace LunchScheduler.Helpers
{
    public static class BackgroundTaskHelpers
    {
        /// <summary>
        /// Sets a new TimeTrigger background task with the supplied parameters
        /// </summary>
        /// <param name="taskFriendlyName">friendly name</param>
        /// <param name="taskEntryPoint">namespace + class name(is also used in app manifest declarations)</param>
        /// <param name="taskRunFrequency">Frequency of background task run</param>
        /// <returns></returns>
        internal static bool RegisterAsync(string taskFriendlyName, string taskEntryPoint, uint taskRunFrequency)
        {
            try
            {
                //if task already exists, unregister it before adding it
                foreach (var task in BackgroundTaskRegistration.AllTasks.Where(cur => cur.Value.Name == taskFriendlyName))
                {
                    task.Value.Unregister(true);
                }

                var builder = new BackgroundTaskBuilder();
                builder.Name = taskFriendlyName;
                builder.TaskEntryPoint = taskEntryPoint;
                builder.SetTrigger(new TimeTrigger(taskRunFrequency, false));
                builder.Register();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BackgroundTaskHelpers RegisterAsync Exception: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Checks if background task already exists
        /// </summary>
        /// <param name="taskFriendlyName"></param>
        /// <returns>True if task exists</returns>
        internal static async Task<bool> CheckBackgroundTasksAsync(string taskFriendlyName)
        {
            try
            {
                await BackgroundExecutionManager.RequestAccessAsync();

                return BackgroundTaskRegistration.AllTasks.Any(task => task.Value.Name == taskFriendlyName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BackgroundTaskHelpers CheckBackgroundTasksAsync Exception: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Removes the background task
        /// </summary>
        /// <param name="taskFriendlyName"></param>
        /// <returns>True if the removal was successful</returns>
        internal static async Task<bool> UnregisterTaskAsync(string taskFriendlyName)
        {
            try
            {
                await BackgroundExecutionManager.RequestAccessAsync();

                foreach (var task in BackgroundTaskRegistration.AllTasks.Where(cur => cur.Value.Name == taskFriendlyName))
                {
                    task.Value.Unregister(true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BackgroundTaskHelpers UnregisterTaskAsync Exception: {ex}");
                return false;
            }
        }
    }
}
