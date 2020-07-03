using System;
using System.ServiceProcess;
using System.Threading;

namespace IncomeService
{
    public partial class IncomeService : ServiceBase
    {
        public IncomeService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DateTime startTime = DateTime.Now;
            TimerCallback dailyCallBack = new TimerCallback(Program.dailyIncome);
            TimerCallback recalcCallBack = new TimerCallback(Program.Recalculating);
            var timeOfDay = DateTime.Now.TimeOfDay;
            var nextFullHour = TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours));
            double delta = (nextFullHour - timeOfDay+TimeSpan.FromMinutes(30)).TotalMilliseconds; 
            Program.dailyTimer = new System.Threading.Timer(dailyCallBack, "",
            Convert.ToInt64(delta), 1000 * 60 * 60);
            Program.fullBalanceRecalculationTimer = new System.Threading.Timer(recalcCallBack, "",
            1000*60, 1000 * 60 * 60*4);
        }

        protected override void OnStop()
        {
        }
    }
}
