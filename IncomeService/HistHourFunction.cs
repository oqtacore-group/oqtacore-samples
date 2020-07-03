using System;
using System.Collections.Generic;
using System.Linq;
using FlyMining.Models;

namespace IncomeService
{
    static class HistHourFunction
    {
        public static void SaveHashrateHistory()
        {
            using (FlyMiningMonitorEntities1 fme = new FlyMiningMonitorEntities1())
            {
                using (FlyMiningEntityDI fdb = new FlyMiningEntityDI())
                {
                    DateTime currentTime = DateTime.UtcNow;
                    currentTime = currentTime.Date.AddHours(currentTime.Hour);
                    DateTime timeHourEarly = currentTime.AddHours(-1);
                    List<MiningMonitorUser> userList = fme.MiningMonitorUsers.Where(t => t.AdminProperty.HasValue && t.AdminProperty.Value && t.LastDataImport> timeHourEarly).ToList();
                    foreach (MiningMonitorUser user in userList)
                    {
                        if (!fdb.HistoricalHourHashrates.Any(t=>t.date==currentTime && t.userID== user.UserID))
                        {
                            fdb.HistoricalHourHashrates.Add(new HistoricalHourHashrate() {
                                date = currentTime,
                                Hashrate = Convert.ToInt64(user.TotalHashrate.Value),
                                MinerAllCount = user.AllMiners,
                                NotWorkingCount = user.AllMiners - user.WorkingMiners,
                                WorkingCount = user.WorkingMiners,
                                userID = user.UserID,
                                Site = true
                            });
                        }
                    }
                    fdb.SaveChanges();
                }
            }
        }
    }
}
