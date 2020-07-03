using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using FlyMining.ApiRequests;
using FlyMining.Models;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IncomeService
{
    static class Program
    {
        static List<string> newFiles = new List<string>();
        static public System.Threading.Timer dailyTimer;
        static public System.Threading.Timer fullBalanceRecalculationTimer;
        static Decimal maintenceRatePerTHsEUR;
        static Decimal rateBTCEUR;
        static Decimal maintenceRatePerTHsBTC;
        static bool balanceLock = false;

        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new IncomeService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static void timeOffsetCheck(IFlyMiningEntityDI fDb)
        {
            try
            {
                if (fDb.UserProfile2.Any(t =>
                    (t.TimeZone == null || (t.TimeZone != null && t.TimeZone == "")) && t.userIP != "::1"))
                {
                    List<UserProfile2> userlist = fDb.UserProfile2.Where(t =>
                        (t.TimeZone == null || (t.TimeZone != null && t.TimeZone == "")) && t.userIP != "::1").ToList();
                    foreach (UserProfile2 user in userlist)
                    {
                        GeoData myLocation = GeoIP.GetCountryInfo(user.userIP);
                        if (myLocation == null || myLocation.timeZone == "")
                        {
                            myLocation = GeoIP.GetCountryInfoCheck(user.userIP);
                        }
                        if (myLocation != null)
                        {
                            user.TimeOffset = myLocation.timeOffset;
                            user.TimeZone = myLocation.timeZone;
                            if (user.TimeZone.Length >= 50)
                            {
                                user.TimeZone = myLocation.timeZone.Substring(0, 50);
                            }
                        }
                    }
                    fDb.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Logger.AddLogRecord("timeOffsetCheck " + Convert.ToString(ex));
            }
        }

        public static void Recalculating(object obj = null)
        {
            using (FlyMiningEntityDI fDB = new FlyMiningEntityDI())
            {
                recalculateBalance(fDB);
                recalculateUserContractHashrate(fDB);
            }
        }

        public static void dailyIncome(object obj = null)
        {
            if (Math.Abs((DateTime.UtcNow - DateTime.UtcNow.Date).TotalMinutes) < 7 || Math.Abs((DateTime.UtcNow - DateTime.UtcNow.Date.AddDays(1)).TotalMinutes) < 7)
            {
                System.Threading.Thread.Sleep(TimeSpan.FromMinutes(10));
            }
            using (FlyMiningEntityDI fDB = new FlyMiningEntityDI())
            {
                calculateNewChanges(fDB, SiteContent.FlyHodler);
                calculateNewChanges(fDB, SiteContent.Flymining);
                calculateFee(fDB);
                timeOffsetCheck(fDB);
                MinningPool.savePoolHashrateHistoryUpdate().Wait();
                MinningPool.savePoolIncomeHistoryUpdate().Wait();
                HistHourFunction.SaveHashrateHistory();
            }
        }

        public static void calculateNewChanges(IFlyMiningEntityDI fDb, SiteContent _siteSelector)
        {
            maintenceRatePerTHsEUR = Settings.maintenceRatePerTHsEUR;
            rateBTCEUR = Math.Round(Convert.ToDecimal(Wallets.getEURrateNow("BTC").Result), 2);
            maintenceRatePerTHsBTC = maintenceRatePerTHsEUR / rateBTCEUR;
            statisticalData statData = new statisticalData();
            try
            {
                statData = fDb.statisticalDatas.Single(t => t.Names == "maintenceRatePerTHsBTC");
                statData.Value = maintenceRatePerTHsBTC.ToString();
                statData = fDb.statisticalDatas.Single(t => t.Names == "BTCRateEUR");
                statData.Value = rateBTCEUR.ToString();
                statData = fDb.statisticalDatas.Single(t => t.Names == "difficultyRaise");
                statData.Value = WebApi.getDifficulty().Result;
                fDb.SaveChanges();
                CryptoCompareInfo.getCryptoInfo();
            }
            catch (Exception ex)
            {
                Logger.AddLogRecord("Error on dayly Income. StatData:" + Convert.ToString(ex));
            }
            RsContractTable(fDb, _siteSelector);
            IncomeValidation(fDb, _siteSelector);
            exportIncome(fDb, _siteSelector);
        }

        static void exportIncome(IFlyMiningEntityDI fDb, SiteContent _siteSelector)
        {
            MailSender mailSender = new MailSender(fDb, _siteSelector, new FlySmtpClient());
            List<ExportIncome> exportList = new List<ExportIncome>();
            List<RsBankPayout> payoutList = new List<RsBankPayout>();
            List<ExportHistory> historyList = new List<ExportHistory>();
            List<ReferralPayment> refPaymentsList = new List<ReferralPayment>();
            var requests = Settings.PaymentRequestListAll(fDb, _siteSelector).Where(t => t.Valid == true && t.Confirmed == true);
            List<UserProfile2> users_list = fDb.UserProfile2.Where(t => requests.Any(y => y.userId == t.Id)).ToList();
            foreach (PaymentRequest request in requests.ToList())
            {
                Decimal payoutFee = 0;
                try
                {
                    if (request.Type != null)
                    {
                        payoutFee = Settings.withdrawFee(request.Type, request.CryptoCurrency);
                    }
                    else
                    {
                        request.Type = "R1";
                        payoutFee = Settings.withdrawFee(request.Type, request.CryptoCurrency);
                    }
                    UserProfile2 user = users_list.SingleOrDefault(t => t.Id == request.userId);
                    List<RsBankAccountBalanceView> accountBalanceList = Settings.rsBalanceList(fDb, user, _siteSelector).Where(t => t.Currency == request.CryptoCurrency).ToList();
                    if (user == null)
                    {
                        Logger.AddLogRecord("Invalid request.Wrong userid " + request.userId.ToString());
                        fDb.PaymentRequests.Remove(request);
                        continue;
                    }
                    if (request.WalletDestinision == "")
                    {
                        Logger.AddLogRecord("Invalid request.Invalid wallet  " + request.WalletDestinision.ToString());
                        fDb.PaymentRequests.Remove(request);
                        continue;
                    }
                    decimal currentBalance = 0;
                    UserBalanceData balanceData = fDb.UserBalanceDatas.SingleOrDefault(t => t.userId == user.Id);
                    if (balanceData == null)
                    {
                        continue;
                    }
                    currentBalance = balanceData.Balance(_siteSelector, request.CryptoCurrency);
                    if (currentBalance < payoutFee)
                    {
                        Logger.AddLogRecord(currentBalance + "<" + payoutFee + " for paymentRequest " + request.id);
                        request.Valid = false;
                        continue;
                    }
                    foreach (RsBankAccountBalanceView account in accountBalanceList.Where(t => t.Type == "contract"))
                    {
                        Contract contract = fDb.Contracts.SingleOrDefault(t => t.TxID == account.RsTxId);
                        if (contract == null)
                        {
                            continue;
                        }
                        ExportHistory newHistory = new ExportHistory(request.CryptoCurrency)
                        {
                            Type = 2,
                            UserId = user.Id,
                            income = 0,
                            ContractID = contract.id,
                            hashrate = contract.hashrate,
                            Referral = false
                        };
                        newHistory.income = account.AmountIncome ?? 0;
                        ExportIncome newExport = new ExportIncome(request.CryptoCurrency)
                        {
                            userId = user.Id,
                            Wallet = request.WalletDestinision,
                            RequestID = request.id,
                            Referral = false,
                            ContractID = contract.id,
                            Sum = account.Amount.Value
                        };
                        if (newExport.Sum < 0)
                        {
                            Logger.AddLogRecord("newExport less then 0 : Income =" + newExport.Sum.ToString(new CultureInfo("en")));
                            continue;
                        }
                        int selector = Settings.ConvertSelectorToInt(_siteSelector);
                        var MaintenanceData = fDb.MaintenanceHistories.Where(t => t.UserId == user.Id && t.Confirmed == true &&
                        t.CryptoCurrency == contract.CryptoCurrency && (t.Contract.ContractType.SiteSelector == selector || (t.NonBTCBalance.HasValue && t.NonBTCBalance.Value == true))).Where(t => t.ContractID == contract.id && t.Spent == false && t.Referral == false);
                        foreach (MaintenanceHistory oper in MaintenanceData.ToList())
                        {
                            oper.Spent = true;
                        }
                        foreach (HodlContractManagementFee oper in fDb.HodlContractManagementFees.Where(t => MaintenanceData.Any(x => x.id == t.MaintId)).ToList())
                        {
                            oper.Spent = true;
                        }
                        exportList.Add(newExport);
                        historyList.Add(newHistory);
                        RsBankPayout newPayout = new RsBankPayout(request);
                        newPayout.Wallet = account.RsTxId;
                        newPayout.Sum_Value = (Math.Round(newExport.Sum * 1000000, 2)).ToString(new CultureInfo("en"));
                        newPayout.Description = newPayout.Sum_Value.ToString(new CultureInfo("en")) + " to " + newPayout.UserWallet + " " + request.CryptoCurrency + " from " + user.RsBankId;
                        payoutList.Add(newPayout);
                    }
                    foreach (RsBankAccountBalanceView account in accountBalanceList.Where(t => t.Type == "contractReferral"))
                    {
                        ContractReferral contractRef = Settings.RefContractList(fDb, SiteContent.Flymining).SingleOrDefault(t => t.TxID == account.RsTxId);
                        if (contractRef == null)
                        {
                            continue;
                        }
                        ExportIncome newExport = new ExportIncome(request.CryptoCurrency)
                        {
                            userId = user.Id,
                            Wallet = request.WalletDestinision,
                            RequestID = request.id,
                            Referral = true,
                            ContractID = contractRef.id,
                            Sum = account.Amount.Value
                        };
                        ExportHistory newHistory = new ExportHistory(request.CryptoCurrency)
                        {
                            Type = 2,
                            UserId = user.Id,
                            ContractID = contractRef.ContractFatherId,
                            hashrate = contractRef.hashrate,
                            Referral = true,
                            income = account.AmountIncome.Value
                        };
                        if (newExport.Sum < 0)
                        {
                            Logger.AddLogRecord("newExport less then 0 : Income =" + newExport.Sum.ToString(new CultureInfo("en")));
                            continue;
                        }
                        exportList.Add(newExport);
                        historyList.Add(newHistory);
                        RsBankPayout newPayout = new RsBankPayout(request)
                        {
                            Wallet = account.RsTxId,
                            Sum_Value = (Math.Round(newExport.Sum * 1000000, 2)).ToString(new CultureInfo("en"))
                        };
                        newPayout.Description = newPayout.Sum_Value.ToString(new CultureInfo("en")) + " to " + newPayout.UserWallet + " " + request.CryptoCurrency + " referral from " + user.RsBankId;
                        payoutList.Add(newPayout);
                    }
                    foreach (RsBankAccountBalanceView account in accountBalanceList.Where(t => t.Type == "ReferralPayment"))
                    {
                        RsBankPayout newPayout = new RsBankPayout(request)
                        {
                            Wallet = user.RsBankId,
                            Sum_Value = (Math.Round(account.RsAmount.Value, 2)).ToString(new CultureInfo("en"))
                        };
                        newPayout.Description = "Referral payment " + newPayout.Sum_Value.ToString(new CultureInfo("en")) + " to " + newPayout.UserWallet + " BTC referral from " + user.RsBankId;
                        payoutList.Add(newPayout);
                        refPaymentsList.Add(fDb.ReferralPayments.Single(t => t.TxId == account.RsTxId));
                        ExportIncome newExport = new ExportIncome(request, account.Amount.Value, true);
                        exportList.Add(newExport);
                    }
                    request.Valid = false;
                    decimal currentCommision = payoutFee;
                    decimal payoutSum = payoutList.Where(t => t.TxID == request.UniqueID).Sum(t => Convert.ToDecimal(t.Sum_Value));
                    request.Amount = payoutSum - currentCommision * 1000000;
                    List<RsBankPayout> commList = new List<RsBankPayout>();
                    foreach (RsBankPayout payout in payoutList.Where(t => t.TxID == request.UniqueID))
                    {
                        RsBankPayout newPayout = new RsBankPayout
                        {
                            UniqueID = Guid.NewGuid().ToString(),
                            Date = DateTime.UtcNow,
                            TxID = payout.TxID,
                            Oper_Type = "5",
                            Wallet_Type = payout.Wallet_Type,
                            Wallet = payout.Wallet,
                            Currency_Name = request.CryptoCurrency,
                            Special_Kind = "S",
                            Rest = "0",
                            Spent = false,
                            UserWallet = payout.UserWallet
                        };
                        newPayout.Year = newPayout.Date.Year;
                        newPayout.Month = newPayout.Date.Month;
                        newPayout.Day = newPayout.Date.Day;
                        currentCommision -= Math.Round((Convert.ToDecimal(payout.Sum_Value) * payoutFee) / payoutSum, 8);
                        if (currentCommision < 0)
                        {
                            currentCommision += Math.Round((Convert.ToDecimal(payout.Sum_Value) * payoutFee) / payoutSum, 8);
                            newPayout.Sum_Value = Convert.ToString(Math.Round(-currentCommision * 1000000, 2), new CultureInfo("en"));
                            payout.Sum_Value = (Convert.ToDecimal(payout.Sum_Value) + Convert.ToDecimal(newPayout.Sum_Value)).ToString("#0.00", CultureInfo.InvariantCulture);
                            currentCommision = 0;
                        }
                        else
                        {
                            newPayout.Sum_Value = Convert.ToString(Math.Round(-(Convert.ToDecimal(payout.Sum_Value) * payoutFee * 1000000) / payoutSum, 2), new CultureInfo("en"));
                            payout.Sum_Value = (Convert.ToDecimal(payout.Sum_Value) + Convert.ToDecimal(newPayout.Sum_Value)).ToString("#0.00", CultureInfo.InvariantCulture);
                        }
                        newPayout.Description = newPayout.Sum_Value.ToString(new CultureInfo("en")) + " " + request.CryptoCurrency + " commision to " + newPayout.UserWallet + " user " + user.RsBankId;
                        commList.Add(newPayout);
                    }
                    payoutList.AddRange(commList);
                    if (currentCommision != 0)
                    {
                        string firstWallet = payoutList.Where(t => t.TxID == request.UniqueID).OrderByDescending(t => t.Sum_Value).First().Wallet;
                        foreach (RsBankPayout payout in payoutList.Where(t => t.Wallet == firstWallet && t.TxID == request.UniqueID))
                        {
                            if (payout.Special_Kind == "S")
                            {
                                payout.Sum_Value = Convert.ToString(Convert.ToDecimal(payout.Sum_Value) + currentCommision, new CultureInfo("en"));
                                payout.Description = payout.Sum_Value.ToString(new CultureInfo("en")) + " " + request.CryptoCurrency + " commision to " + payout.UserWallet + " user " + user.RsBankId;
                            }
                            else
                            {
                                payout.Sum_Value = Convert.ToString(Convert.ToDecimal(payout.Sum_Value) - currentCommision, new CultureInfo("en"));
                                payout.Description = payout.Sum_Value.ToString(new CultureInfo("en")) + " to " + payout.UserWallet + " " + request.CryptoCurrency + " from " + user.RsBankId;
                            }
                        }
                    }
                    mailSender.SendPaymentRequest(user, request);
                }
                catch (Exception ex)
                {
                    Logger.AddLogRecord("Payment Request Parsing.Error:" + Convert.ToString(ex));
                }
            }
            foreach (ExportIncome row in exportList)
            {
                fDb.ExportIncomes.Add(row);
            }
            foreach (RsBankPayout row in payoutList)
            {
                fDb.RsBankPayouts.Add(row);
            }
            foreach (ExportHistory row in historyList)
            {
                fDb.ExportHistories.Add(row);
            }
            foreach (ReferralPayment row in refPaymentsList)
            {
                row.Sent = true;
            }
            try
            {
                fDb.SaveChanges();
                UtilityFunc utilHandler = new UtilityFunc(fDb);
                foreach (UserProfile2 user in users_list)
                {
                    utilHandler.recalculateBalance(user);
                }
            }
            catch (Exception ex)
            {
                Logger.AddLogRecord("Error on SaveChanges Export Income Service :" + Convert.ToString(ex));

            }
        }

        static void recalculateReferralsContracts()
        {
            using (FlyMiningEntityDI Fdb = new FlyMiningEntityDI())
            {
                List<UserProfile2> userList = Fdb.UserProfile2.Where(t => t.InvitedBy.HasValue && t.InvitedBy != t.Id && t.InvitedBy != 0).ToList();
                List<ContractReferral> referralList = Fdb.ContractReferrals.ToList();
                List<Contract> contractList = Fdb.Contracts.ToList();
                decimal referralPart = Settings.FriendPart;
                foreach (UserProfile2 user in userList)
                {
                    List<Contract> contractUserList = contractList.Where(t => t.userID == user.Id).ToList();
                    foreach (Contract contract in contractUserList)
                    {
                        if (referralList.Count(t => t.userID == user.Id && t.ContractFatherId == contract.id) == 0)
                        {
                            ContractReferral newRefContr = new ContractReferral(contract, user.Id, referralPart);
                            Fdb.ContractReferrals.Add(newRefContr);
                        }
                        if (referralList.Count(t => t.userID == user.InvitedBy && t.ContractFatherId == contract.id) == 0)
                        {
                            ContractReferral newRefContr = new ContractReferral(contract, user.Id, referralPart);
                            Fdb.ContractReferrals.Add(newRefContr);
                        }
                    }
                    Fdb.SaveChanges();
                }
            }
        }

        static void recalculateUserContractHashrate(IFlyMiningEntityDI fDb)
        {
            var userList = fDb.UserProfile2;
            foreach (UserProfile2 user in userList.ToList())
            {
                user.CurrentContractHashrate = 0;
                var contractList = Settings.activeContractList(fDb, SiteContent.Flymining, false).Where(t => t.userID == user.Id);
                if (contractList.Any())
                {
                    user.CurrentContractHashrate += contractList.Sum(t => t.hashrate);
                }
                contractList = Settings.activeContractList(fDb, SiteContent.FlyHodler, false).Where(t => t.userID == user.Id);
                if (contractList.Any())
                {
                    user.CurrentContractHashrate += contractList.Sum(t => t.hashrate);
                }
            }
            fDb.SaveChanges();
        }

        static void IncomeValidation(IFlyMiningEntityDI fDB, SiteContent _siteSelector)
        {
            var exportList = Settings.exportHistoryAll(fDB, _siteSelector).Where(t => (!t.Confirmed.HasValue || t.Confirmed.Value == false) &&
                 fDB.Contracts.Any(x => x.id == t.ContractID && (x.Status == 4 || x.Status == 7)));
            if (exportList.Any())
            {
                foreach (ExportHistory export in exportList.ToList())
                {
                    export.Confirmed = true;
                }
                fDB.SaveChanges();
            }
            var exportPurgeList = Settings.exportHistoryAll(fDB, _siteSelector).Where(t => (t.Confirmed.HasValue && t.Confirmed.Value == true) &&
                    fDB.Contracts.Any(x => x.id == t.ContractID && (x.Status != 4 && x.Status != 7 && x.Status != 8 && x.Status != 9)));
            if (exportPurgeList.Any())
            {
                foreach (ExportHistory export in exportPurgeList.ToList())
                {
                    export.Confirmed = false;
                }
                fDB.SaveChanges();
            }
            var maintPurgeList = Settings.mainHistoryAll(fDB, _siteSelector).Where(t => (!t.Confirmed.HasValue || t.Confirmed == true) && fDB.Contracts.Any(x => x.id == t.ContractID
                 && (x.Status != 4 && x.Status != 7 && x.Status != 8 && x.Status != 9)));
            if (maintPurgeList.Any())
            {
                foreach (MaintenanceHistory maint in maintPurgeList.ToList())
                {
                    maint.Confirmed = false;
                }
                fDB.SaveChanges();
            }
            var maintList = Settings.mainHistoryAll(fDB, _siteSelector).Where(t => (!t.Confirmed.HasValue || t.Confirmed == false)
                && fDB.Contracts.Any(x => x.id == t.ContractID && (x.Status == 4 || x.Status == 7)));
            if (maintList.Any())
            {
                foreach (MaintenanceHistory maint in maintList.ToList())
                {
                    maint.Confirmed = true;
                }
                fDB.SaveChanges();
            }
        }

        static void recalculateBalance(IFlyMiningEntityDI fDb)
        {
            if (!balanceLock)
            {
                balanceLock = true;
                UtilityFunc utilityHandler = new UtilityFunc(fDb);
                utilityHandler.fillRsBankPairedBillOperation(SiteContent.FlyHodler);
                utilityHandler.fillRsBankPairedBillOperation(SiteContent.Flymining);
                List<UserProfile2> userList = fDb.UserProfile2.Where(t => fDb.RsBankAccountBalanceViews.Any(x => x.userId == t.Id && x.AmountIncome > 0) || fDb.UserBalanceDatas.Any(y => y.userId == t.Id && (y.BalanceBTC > 0 || y.BalanceETH > 0 || y.BalanceHodlBTC > 0))).ToList();
                List<UserBalanceData> balanceList = fDb.UserBalanceDatas.ToList().Where(t => userList.Any(x => x.Id == t.userId)).ToList();
                try
                {
                    foreach (UserProfile2 user in userList)
                    {
                        var accountBalanceFlyminingList = Settings.rsBalanceList(fDb, user, SiteContent.Flymining);
                        if (accountBalanceFlyminingList.Any(t => t.Currency == "BTC"))
                        {
                            user.BalanceBTC = accountBalanceFlyminingList.Where(t => t.Currency == "BTC")
                                .Sum(t => t.AmountIncome);
                            user.TotalBalanceBTC = accountBalanceFlyminingList.Where(t => t.Currency == "BTC")
                                .Sum(t => t.totalIncome);
                            user.MaintanceBTC = accountBalanceFlyminingList.Where(t => t.Currency == "BTC")
                                .Sum(t => t.AmountMaint);
                            user.TotalMaintanceBTC = accountBalanceFlyminingList.Where(t => t.Currency == "BTC")
                                .Sum(t => t.totalMaint);
                        }
                        else
                        {
                            user.BalanceBTC = 0;
                            user.TotalBalanceBTC = 0;
                            user.MaintanceBTC = 0;
                            user.TotalMaintanceBTC = 0;
                        }
                        var maintPureList = Settings.mainHistoryWithCommon(fDb, user, SiteContent.Common, "BTC");
                        user.MaintenanceBalance = maintPureList.Any() ? -1 * maintPureList.Sum(t => t.maintenancePure) : 0;
                        var maintOperList = fDb.UserMaintenanceBalanceOperations.Where(t => t.Status == 4 && t.UserId == user.Id);
                        user.MaintenanceBalance += maintOperList.Any() ? maintOperList.Sum(t => t.Balance) : 0;
                        UserBalanceData balanceData = balanceList.SingleOrDefault(t => t.userId == user.Id);
                        if (balanceData == null)
                        {
                            balanceData = new UserBalanceData(user.Id);
                            fDb.UserBalanceDatas.Add(balanceData);
                        }
                        balanceData.MaintenanceBTC = user.MaintanceBTC ?? 0;
                        balanceData.BalanceBTC = user.BalanceBTC ?? 0;
                        if (accountBalanceFlyminingList.Any(t => t.Currency == "ETH"))
                        {
                            balanceData.BalanceETH = accountBalanceFlyminingList.Where(t => t.Currency == "ETH")
                                .Sum(t => t.AmountIncome.Value);
                        }
                        else
                        {
                            balanceData.BalanceETH = 0;
                        }
                        var accountBalanceFlyHodlList = Settings.rsBalanceList(fDb, user, SiteContent.FlyHodler).Where(t => t.Currency == "BTC");
                        if (accountBalanceFlyHodlList.Any())
                        {
                            balanceData.BalanceHodlBTC = accountBalanceFlyHodlList.Sum(t => t.AmountIncome);
                            balanceData.MaintenanceHodlBTC = accountBalanceFlyHodlList.Sum(t => t.AmountMaint);
                        }
                        else
                        {
                            balanceData.BalanceHodlBTC = 0;
                            balanceData.MaintenanceHodlBTC = 0;
                        }
                    }
                    fDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.AddLogRecord(Convert.ToString(ex));
                }
                balanceLock = false;
            }
        }

        static async void calculateFee(IFlyMiningEntityDI fDB)
        {
            var stringPayload = JsonConvert.DeserializeObject<JToken>(await Wallets.sendAsyncRequest("", "https://bitcoinfees.earn.com/api/v1/fees/recommended").ConfigureAwait(false));
            string result = stringPayload["hourFee"].ToString();
            try
            {
                statisticalData statData = fDB.statisticalDatas.Single(t => t.Names == "minerFee");
                statData.Value = result;
                fDB.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.AddLogRecord("calculate Fee|IncomeService:" + Convert.ToString(ex));
            }
        }

        static void RsContractTable(IFlyMiningEntityDI fDb, SiteContent _siteSelector)
        {
            try
            {
                var contractRsDataList = fDb.RsBankNewContracts.Where(t => !t.Bill_Id.HasValue || fDb.Users_Bills.Any(x => x.Id == t.Bill_Id && x.BillStatusId == 4));
                var contractList = Settings.ContractList(fDb, _siteSelector);
                var userBillsList = Settings.UserBillsList(fDb, _siteSelector, false);
                var operList = fDb.RsBankMaintBalanceOpers;
                var maintBalance = fDb.UserMaintenanceBalanceOperations;
                foreach (Contract contract in contractList.ToList())
                {
                    if (contractRsDataList.Any(t => t.ContractID == contract.id && t.Referral == false && t.ContractStatus != contract.Status))
                    {
                        List<RsBankNewContract> rowList = contractRsDataList.Where(t => t.ContractID == contract.id && t.Referral == false && t.ContractStatus != contract.Status).ToList();
                        foreach (RsBankNewContract row in rowList)
                        {
                            row.ContractStatus = contract.Status;
                        }
                    }
                }
                foreach (UserMaintenanceBalanceOperation maintRow in maintBalance.ToList())
                {
                    if (operList.Any(t => t.OperID == maintRow.id && t.OperStatus != maintRow.Status))
                    {
                        foreach (RsBankMaintBalanceOper row in operList.Where(t => t.OperID == maintRow.id && t.OperStatus != maintRow.Status).ToList())
                        {
                            row.OperStatus = maintRow.Status;
                        }
                    }
                    if (!operList.Any(t => t.OperID == maintRow.id))
                    {
                        UserProfile2 currentUser = fDb.UserProfile2.Single(t => t.Id == maintRow.UserId);
                        if (currentUser.MaintenanceBalanceId == null || currentUser.MaintenanceBalanceId == "")
                        {
                            currentUser.MaintenanceBalanceId = Guid.NewGuid().ToString();
                        }
                        Users_Bills bill = userBillsList.SingleOrDefault(t => t.BalanceId == maintRow.id);
                        if (bill != null)
                        {
                            fDb.RsBankMaintBalanceOpers.Add(new RsBankMaintBalanceOper(maintRow, "PlaceHolder", currentUser.RsBankId));
                        }
                    }
                }
                fDb.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.AddLogRecord("RsContractTable +" + Convert.ToString(ex));
            }
        }
    }
}
