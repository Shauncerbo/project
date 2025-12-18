using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using project.Data;
using project.Models;

namespace project.Services
{
    public interface IDatabaseSyncService
    {
        Task<SyncResult> SyncBidirectionalAsync();
        Task<bool> SyncToLocalAsync();
        Task<bool> SyncToOnlineAsync();
        Task<SyncResult> SyncOnlineToLocalAsync();
        Task<string> GetCurrentDatabaseTypeAsync();
        Task<Dictionary<string, int>> GetLocalCountsAsync();
        Task<Dictionary<string, int>> GetOnlineCountsAsync();
        Task<DateTime?> GetLastSyncTimeAsync();
    }

    public class DatabaseSyncService : IDatabaseSyncService
    {
        private const string LOCAL_CONNECTION_STRING = 
            "Data Source=LAPTOP-3VCGD3TV\\SQLEXPRESS;Initial Catalog=GymCRM_DB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

        private const string ONLINE_CONNECTION_STRING = 
            "Server=db32884.public.databaseasp.net;Database=db32884;User Id=db32884;Password=P_y79xY!kQ%6;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        public async Task<string> GetCurrentDatabaseTypeAsync()
        {
            try
            {
                var dbType = await SecureStorage.Default.GetAsync("database_type");
                return string.IsNullOrEmpty(dbType) ? "local" : dbType; 
            }
            catch
            {
                return "local";
            }
        }

        public async Task<bool> SyncToOnlineAsync()
        {
            var result = await SyncBidirectionalAsync();
            return result.Success;
        }

        public async Task<bool> SyncToLocalAsync()
        {
            var result = await SyncBidirectionalAsync();
            return result.Success;
        }

        public async Task<SyncResult> SyncOnlineToLocalAsync()
        {
            var result = new SyncResult
            {
                SyncTime = DateTime.Now
            };

            try
            {
                var localOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(LOCAL_CONNECTION_STRING)
                    .Options;

                var onlineOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(ONLINE_CONNECTION_STRING)
                    .Options;

                using var localContext = new AppDbContext(localOptions);
                using var onlineContext = new AppDbContext(onlineOptions);

                if (!await localContext.Database.CanConnectAsync())
                {
                    throw new Exception("Cannot connect to local database");
                }

                // Test online connection
                try
                {
                    if (!await onlineContext.Database.CanConnectAsync())
                    {
                        throw new Exception("Cannot connect to online database");
                    }
                }
                catch (Exception connEx)
                {
                    throw new Exception($"Cannot connect to online database: {connEx.Message}", connEx);
                }

                // Only download from online to local (no upload)
                await SyncTableOnlineToLocal<Role>(localContext, onlineContext, result);
                await SyncTableOnlineToLocal<RolePermission>(localContext, onlineContext, result);
                await SyncTableOnlineToLocal<MembershipType>(localContext, onlineContext, result);
                
                // Clean up duplicate promotions before syncing
                await CleanupDuplicatePromotions(localContext, result);
                
                await SyncTableOnlineToLocal<Promotion>(localContext, onlineContext, result);
                
                // Clean up duplicate promotions AFTER syncing (in case sync created new duplicates)
                await CleanupDuplicatePromotions(localContext, result);
                
                // Clean up duplicate trainers before syncing
                await CleanupDuplicateTrainers(localContext, result);
                
                await SyncTableOnlineToLocal<Trainer>(localContext, onlineContext, result);
                await SyncTableOnlineToLocal<User>(localContext, onlineContext, result);
                await SyncTableOnlineToLocal<TrainerSchedule>(localContext, onlineContext, result);
                
                // Clean up duplicate members before syncing
                await CleanupDuplicateMembers(localContext, result);
                
                await SyncTableOnlineToLocal<Member>(localContext, onlineContext, result);
                
                // Clean up duplicate walk-ins before syncing
                await CleanupDuplicateWalkIns(localContext, result);
                
                await SyncTableOnlineToLocal<WalkIn>(localContext, onlineContext, result);
                await SyncTableOnlineToLocal<Attendance>(localContext, onlineContext, result);
                await SyncTableOnlineToLocal<Payment>(localContext, onlineContext, result);
                
                // Sync MemberPromo AFTER Members and Promotions
                await SyncTableOnlineToLocal<MemberPromo>(localContext, onlineContext, result);
                
                // Sync MemberTrainer AFTER Members and Trainers
                await SyncTableOnlineToLocal<MemberTrainer>(localContext, onlineContext, result);
                
                // Sync Notifications AFTER Members
                await SyncTableOnlineToLocal<Notification>(localContext, onlineContext, result);
                
                // Sync AuditLogs last
                await SyncTableOnlineToLocal<AuditLog>(localContext, onlineContext, result);

                await localContext.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Successfully downloaded {result.TotalDownloaded} records and updated {result.TotalUpdated} records from online to local database.";

                // Save sync time
                await SecureStorage.Default.SetAsync("last_sync_time", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"SyncOnlineToLocalAsync error: {ex.Message}");
            }

            return result;
        }

        public async Task<SyncResult> SyncBidirectionalAsync()
        {
            var result = new SyncResult
            {
                SyncTime = DateTime.Now
            };

            try
            {
                var localOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(LOCAL_CONNECTION_STRING)
                    .Options;

                var onlineOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(ONLINE_CONNECTION_STRING)
                    .Options;

                using var localContext = new AppDbContext(localOptions);
                using var onlineContext = new AppDbContext(onlineOptions);

                if (!await localContext.Database.CanConnectAsync())
                {
                    throw new Exception("Cannot connect to local database");
                }

                // Test online connection with detailed error handling
                try
                {
                    if (!await onlineContext.Database.CanConnectAsync())
                    {
                        throw new Exception("Cannot connect to online database - CanConnectAsync returned false");
                    }
                }
                catch (Exception connEx)
                {
                    var errorMsg = connEx.Message;
                    var innerMsg = connEx.InnerException?.Message ?? "";
                    var fullError = $"{errorMsg} {innerMsg}".Trim();
                    
                    // Check for specific SQL error patterns
                    if (fullError.Contains("18456") || fullError.Contains("Login failed"))
                    {
                        throw new Exception("Cannot connect to online database - Login failed. Please verify the username and password in the connection string.", connEx);
                    }
                    else if (fullError.Contains("timeout") || fullError.Contains("Timeout") || fullError.Contains("timed out"))
                    {
                        throw new Exception("Cannot connect to online database - Connection timeout. The server may be unreachable. Check your internet connection and firewall settings.", connEx);
                    }
                    else if (fullError.Contains("network") || fullError.Contains("Network") || fullError.Contains("provider") || fullError.Contains("Provider"))
                    {
                        throw new Exception("Cannot connect to online database - Network error. Check your internet connection and verify the server address is correct.", connEx);
                    }
                    else if (fullError.Contains("server") || fullError.Contains("Server") || fullError.Contains("cannot be reached"))
                    {
                        throw new Exception($"Cannot connect to online database - Server unreachable. Error: {fullError}", connEx);
                    }
                    else
                    {
                        // Include the full error message for debugging
                        System.Diagnostics.Debug.WriteLine($"Full connection error: {fullError}");
                        System.Diagnostics.Debug.WriteLine($"Exception type: {connEx.GetType().Name}");
                        if (connEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner exception type: {connEx.InnerException.GetType().Name}");
                        }
                        throw new Exception($"Cannot connect to online database: {fullError}", connEx);
                    }
                }

                await SyncTableBidirectional<Role>(localContext, onlineContext, result);
                await SyncTableBidirectional<RolePermission>(localContext, onlineContext, result);
                await SyncTableBidirectional<MembershipType>(localContext, onlineContext, result);
                
                // Clean up duplicate promotions before syncing
                await CleanupDuplicatePromotions(localContext, result);
                await CleanupDuplicatePromotions(onlineContext, result);
                
                await SyncTableBidirectional<Promotion>(localContext, onlineContext, result);
                
                // Clean up duplicate promotions AFTER syncing (in case sync created new duplicates)
                await CleanupDuplicatePromotions(localContext, result);
                await CleanupDuplicatePromotions(onlineContext, result);
                
                // Clean up duplicate trainers before syncing
                await CleanupDuplicateTrainers(localContext, result);
                await CleanupDuplicateTrainers(onlineContext, result);
                
                await SyncTableBidirectional<Trainer>(localContext, onlineContext, result);
                await SyncTableBidirectional<User>(localContext, onlineContext, result);
                await SyncTableBidirectional<TrainerSchedule>(localContext, onlineContext, result);
                
                // Clean up duplicate members before syncing
                await CleanupDuplicateMembers(localContext, result);
                await CleanupDuplicateMembers(onlineContext, result);
                
                await SyncTableBidirectional<Member>(localContext, onlineContext, result);
                
                // Clean up duplicate walk-ins before syncing
                await CleanupDuplicateWalkIns(localContext, result);
                await CleanupDuplicateWalkIns(onlineContext, result);
                
                await SyncTableBidirectional<WalkIn>(localContext, onlineContext, result);
                
                // Clean up duplicate attendances before syncing
                await CleanupDuplicateAttendances(localContext, result);
                await CleanupDuplicateAttendances(onlineContext, result);
                
                await SyncTableBidirectional<Attendance>(localContext, onlineContext, result);
                await SyncTableBidirectional<Payment>(localContext, onlineContext, result);
                
                // Sync MemberPromo AFTER Members and Promotions (it depends on both)
                // MemberPromo has a composite key, so we need to be careful with change tracking
                await SyncTableBidirectional<MemberPromo>(localContext, onlineContext, result);
                
                // Sync MemberTrainer AFTER Members and Trainers
                await SyncTableBidirectional<MemberTrainer>(localContext, onlineContext, result);
                
                // Sync Notifications AFTER Members
                await SyncTableBidirectional<Notification>(localContext, onlineContext, result);
                
                await SyncTableBidirectional<AuditLog>(localContext, onlineContext, result);

                await SecureStorage.Default.SetAsync("last_sync_time", result.SyncTime.ToString("O"));

                result.Success = true;
                result.Message = $"Sync completed successfully! Uploaded: {result.TotalUploaded}, Downloaded: {result.TotalDownloaded}, Updated: {result.TotalUpdated}, Conflicts: {result.TotalConflicts}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in bidirectional sync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }

            return result;
        }

        private async Task SyncTableBidirectional<T>(AppDbContext localContext, AppDbContext onlineContext, SyncResult result) where T : class
        {
            try
            {
                var tableName = typeof(T).Name;
                var summary = new TableSyncSummary { TableName = tableName };

                // CRITICAL: Clear the change tracker completely before starting each table sync
                // This ensures no entities from previous table syncs persist
                localContext.ChangeTracker.Clear();
                onlineContext.ChangeTracker.Clear();
                System.Diagnostics.Debug.WriteLine($"Cleared change tracker before starting {tableName} sync");

                // ALWAYS detach ALL MemberPromo entities before syncing ANY table (defensive)
                // These entities can persist in the change tracker and cause errors during SaveChanges
                DetachAllCompositeKeyEntities(localContext);
                DetachAllCompositeKeyEntities(onlineContext);

                var localSet = localContext.Set<T>();
                var onlineSet = onlineContext.Set<T>();

                var localRecords = await localSet.AsNoTracking().ToListAsync();
                var onlineRecords = await onlineSet.AsNoTracking().ToListAsync();

                // Log Attendance records specifically
                if (typeof(T) == typeof(Attendance))
                {
                    System.Diagnostics.Debug.WriteLine($"=== Attendance Sync: Local={localRecords.Count} records, Online={onlineRecords.Count} records ===");
                    var localAttendances = localRecords.Cast<Attendance>().ToList();
                    var onlineAttendances = onlineRecords.Cast<Attendance>().ToList();
                    var localLatest = localAttendances.OrderByDescending(a => a.CheckinTime).FirstOrDefault();
                    var onlineLatest = onlineAttendances.OrderByDescending(a => a.CheckinTime).FirstOrDefault();
                    if (localLatest != null)
                        System.Diagnostics.Debug.WriteLine($"Local latest: AttendanceID={localLatest.AttendanceID}, MemberID={localLatest.MemberID}, CheckinTime={localLatest.CheckinTime}");
                    if (onlineLatest != null)
                        System.Diagnostics.Debug.WriteLine($"Online latest: AttendanceID={onlineLatest.AttendanceID}, MemberID={onlineLatest.MemberID}, CheckinTime={onlineLatest.CheckinTime}");
                }
                
                // Log Promotion records specifically
                if (typeof(T) == typeof(Promotion))
                {
                    System.Diagnostics.Debug.WriteLine($"=== Promotion Sync: Local={localRecords.Count} records, Online={onlineRecords.Count} records ===");
                    var localPromotions = localRecords.Cast<Promotion>().ToList();
                    var onlinePromotions = onlineRecords.Cast<Promotion>().ToList();
                    foreach (var promo in localPromotions)
                    {
                        System.Diagnostics.Debug.WriteLine($"Local Promotion: PromoID={promo.PromoID}, PromoName={promo.PromoName}, DiscountRate={promo.DiscountRate}");
                    }
                    foreach (var promo in onlinePromotions)
                    {
                        System.Diagnostics.Debug.WriteLine($"Online Promotion: PromoID={promo.PromoID}, PromoName={promo.PromoName}, DiscountRate={promo.DiscountRate}");
                    }
                }

                // Group by ID and take first record to handle duplicates (same ID can exist if data is corrupted)
                var localDict = localRecords
                    .GroupBy(r => GetId(r))
                    .Where(g => g.Key > 0) // Only include valid IDs
                    .ToDictionary(g => g.Key, g => g.First());
                
                var onlineDict = onlineRecords
                    .GroupBy(r => GetId(r))
                    .Where(g => g.Key > 0) // Only include valid IDs
                    .ToDictionary(g => g.Key, g => g.First());
                
                // Log if duplicates were found
                var localDuplicates = localRecords.GroupBy(r => GetId(r)).Where(g => g.Count() > 1 && g.Key > 0).ToList();
                var onlineDuplicates = onlineRecords.GroupBy(r => GetId(r)).Where(g => g.Count() > 1 && g.Key > 0).ToList();
                
                if (localDuplicates.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Found {localDuplicates.Count} duplicate ID groups in local {tableName} table. IDs: {string.Join(", ", localDuplicates.Select(g => g.Key))}");
                }
                
                if (onlineDuplicates.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Found {onlineDuplicates.Count} duplicate ID groups in online {tableName} table. IDs: {string.Join(", ", onlineDuplicates.Select(g => g.Key))}");
                }

                // Upload missing local data to online
                foreach (var localRecord in localRecords)
                {
                    var id = GetId(localRecord);
                    
                    if (id <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping {tableName} record - invalid ID: {id}");
                        continue;
                    }
                    
                    // Fix any invalid foreign key references BEFORE validation
                    await FixForeignKeys(localRecord, onlineContext);
                    
                    if (!await ValidateForeignKeys(localRecord, onlineContext))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping {tableName} record ID {id} - foreign key validation failed");
                        continue;
                    }
                    
                    var existingByUniqueKey = await FindByUniqueKey<T>(localRecord, onlineContext);
                    
                    if (!onlineDict.ContainsKey(id))
                    {
                        if (existingByUniqueKey != null)
                        {
                            try
                            {
                                var existingId = GetId(existingByUniqueKey);
                                var existing = await GetEntityForUpdateAsync(onlineSet, onlineContext, existingId);
                                if (existing != null)
                                {
                                    await FixForeignKeys(localRecord, onlineContext);
                                    onlineContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                    await UpdateEntityValues(existing, localRecord, onlineContext);
                                    summary.Updated++;
                                    result.TotalUpdated++;
                                    System.Diagnostics.Debug.WriteLine($"Updated {tableName} by unique key: Local ID {id} -> Online ID {existingId}");
                                    continue; // Skip to next record
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating {tableName} by unique key: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Record exists only in local - upload to online
                            // BEFORE adding, check for unique key conflicts
                            if (typeof(T) == typeof(User))
                            {
                                var user = localRecord as User;
                                if (user != null && !string.IsNullOrEmpty(user.Username))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Pre-validation: Checking for existing User with username '{user.Username}' in online database");
                                    
                                    // Check if a user with this username already exists in online database
                                    var existingUser = await FindByUniqueKeyValue(user.Username, onlineContext);
                                    
                                    // If not found, try with trimmed and normalized versions
                                    if (existingUser == null)
                                    {
                                        var trimmedUsername = user.Username.Trim();
                                        if (trimmedUsername != user.Username)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Pre-validation: Trying trimmed username '{trimmedUsername}'");
                                            existingUser = await FindByUniqueKeyValue(trimmedUsername, onlineContext);
                                        }
                                    }
                                    
                                    // Final fallback: Direct SQL query
                                    if (existingUser == null)
                                    {
                                        try
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Pre-validation: Trying direct SQL query for username '{user.Username}'");
                                            var sql = "SELECT * FROM Users WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@p0)))";
                                            var sqlUsers = await onlineContext.Users
                                                .FromSqlRaw(sql, new Microsoft.Data.SqlClient.SqlParameter("@p0", user.Username))
                                                .AsNoTracking()
                                                .ToListAsync();
                                            
                                            if (sqlUsers.Any())
                                            {
                                                existingUser = sqlUsers.First();
                                                System.Diagnostics.Debug.WriteLine($"Pre-validation: Found User via direct SQL: '{existingUser.Username}' (ID: {existingUser.UserID})");
                                            }
                                        }
                                        catch (Exception sqlEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Pre-validation: SQL query failed: {sqlEx.Message}");
                                        }
                                    }
                                    
                                    if (existingUser != null)
                                    {
                                        // User exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingUser);
                                            System.Diagnostics.Debug.WriteLine($"Pre-validated: Found existing User '{existingUser.Username}' (ID: {existingId}) - updating instead of inserting");
                                            
                                        // Use the context-tracked entity (or load it) to avoid double-tracking
                                        var existing = await FindEntityByIdAsync(onlineSet, existingId);
                                            if (existing != null)
                                            {
                                                await FixForeignKeys(localRecord, onlineContext);
                                                await UpdateEntityValues(existing, localRecord, onlineContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing User '{user.Username}' (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Pre-validation: Could not load User entity for ID {existingId} from context");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating User by unique key in pre-validation: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Pre-validation: No existing User found with username '{user.Username}' - will attempt insert");
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Trainer))
                            {
                                var trainer = localRecord as Trainer;
                                if (trainer != null)
                                {
                                    var localId = GetId(trainer);
                                    System.Diagnostics.Debug.WriteLine($"Checking Trainer from local (ID: {localId}): '{trainer.FirstName} {trainer.LastName}', Contact: '{trainer.ContactNumber}'");
                                    
                                    // Check if a trainer with same name/contact already exists in online database
                                    var existingTrainer = await FindTrainerByUniqueFields(trainer, onlineContext);
                                    if (existingTrainer != null)
                                    {
                                        var existingId = GetId(existingTrainer);
                                        System.Diagnostics.Debug.WriteLine($"Found matching Trainer in online (ID: {existingId}): '{existingTrainer.FirstName} {existingTrainer.LastName}', Contact: '{existingTrainer.ContactNumber}'");
                                        
                                        // Trainer exists - update it instead of inserting
                                        try
                                        {
                                            var existing = await GetEntityForUpdateAsync(onlineSet, onlineContext, existingId);
                                            if (existing != null)
                                            {
                                                // Detach composite key entities before updating
                                                DetachCompositeKeyEntities(existing, onlineContext);
                                                onlineContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                await UpdateEntityValues(existing, localRecord, onlineContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Trainer '{trainer.FirstName} {trainer.LastName}' (Local ID: {localId} -> Online ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Trainer by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"No matching Trainer found in online for local Trainer ID {localId}: '{trainer.FirstName} {trainer.LastName}', Contact: '{trainer.ContactNumber}' - will insert as new");
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Member))
                            {
                                var member = localRecord as Member;
                                if (member != null)
                                {
                                    // Check if a member with same email/contact already exists in online database
                                    var existingMember = await FindMemberByUniqueFields(member, onlineContext);
                                    if (existingMember != null)
                                    {
                                        // Member exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingMember);
                                            // Use direct update for Members to avoid MemberPromo tracking issues
                                            await FixForeignKeys(localRecord, onlineContext);
                                            if (await UpdateMemberDirectly(member, onlineContext, existingId))
                                            {
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Member '{member.FirstName} {member.LastName}' (Email: {member.Email}) (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Member by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Promotion))
                            {
                                var promotion = localRecord as Promotion;
                                if (promotion != null)
                                {
                                    // Check if a promotion with same name, dates, and discount already exists in online database
                                    var existingPromotion = await FindPromotionByUniqueFields(promotion, onlineContext);
                                    if (existingPromotion != null)
                                    {
                                        // Promotion exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingPromotion);
                                            var existing = await GetEntityForUpdateAsync(onlineSet, onlineContext, existingId);
                                            if (existing != null)
                                            {
                                                await FixForeignKeys(localRecord, onlineContext);
                                                onlineContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                await UpdateEntityValues(existing, localRecord, onlineContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Promotion '{promotion.PromoName}' (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Promotion by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"No matching Promotion found in online for local Promotion ID {GetId(promotion)}: '{promotion.PromoName}' - will insert as new");
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Payment))
                            {
                                var payment = localRecord as Payment;
                                if (payment != null)
                                {
                                    var localId = GetId(payment);
                                    System.Diagnostics.Debug.WriteLine($"Checking Payment from local (ID: {localId}): MemberID={payment.MemberID}, Date={payment.PaymentDate:yyyy-MM-dd}, Amount={payment.Amount}, Type={payment.PaymentType}");
                                    
                                    // Check if a payment with same MemberID, PaymentDate, Amount, and PaymentType already exists in online database
                                    var existingPayment = await FindPaymentByUniqueFields(payment, onlineContext);
                                    if (existingPayment != null)
                                    {
                                        var existingId = GetId(existingPayment);
                                        System.Diagnostics.Debug.WriteLine($"Found matching Payment in online (ID: {existingId}): MemberID={existingPayment.MemberID}, Date={existingPayment.PaymentDate:yyyy-MM-dd}, Amount={existingPayment.Amount}, Type={existingPayment.PaymentType}");
                                        
                                        // Payment exists - update it instead of inserting
                                        try
                                        {
                                            var existing = await GetEntityForUpdateAsync(onlineSet, onlineContext, existingId);
                                            if (existing != null)
                                            {
                                                await FixForeignKeys(localRecord, onlineContext);
                                                onlineContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                await UpdateEntityValues(existing, localRecord, onlineContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Payment (Local ID: {localId} -> Online ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Payment by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"No matching Payment found in online for local Payment ID {localId}: MemberID={payment.MemberID}, Date={payment.PaymentDate:yyyy-MM-dd}, Amount={payment.Amount}, Type={payment.PaymentType} - will insert as new");
                                    }
                                }
                            }
                            
                            // If no unique key conflict found, proceed with insert
                            try
                            {
                                if (typeof(T) == typeof(Member))
                                {
                                    // Use direct insert for Members to avoid MemberPromo tracking issues
                                    var member = localRecord as Member;
                                    if (member != null)
                                    {
                                        await FixForeignKeys(localRecord, onlineContext);
                                        if (await InsertMemberDirectly(member, onlineContext))
                                        {
                                            summary.Uploaded++;
                                            result.TotalUploaded++;
                                        }
                                    }
                                }
                                else
                            {
                                // Final check for User entities - make absolutely sure we don't have a duplicate
                                if (typeof(T) == typeof(User))
                                {
                                    var user = localRecord as User;
                                    if (user != null && !string.IsNullOrEmpty(user.Username))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Final check before adding User: '{user.Username}'");
                                        
                                        // Try one more time with direct SQL query
                                        try
                                        {
                                            var sql = "SELECT * FROM Users WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@p0)))";
                                            var existingUsers = await onlineContext.Users
                                                .FromSqlRaw(sql, new Microsoft.Data.SqlClient.SqlParameter("@p0", user.Username))
                                                .AsNoTracking()
                                                .ToListAsync();
                                            
                                            if (existingUsers.Any())
                                            {
                                                var existingUser = existingUsers.First();
                                                System.Diagnostics.Debug.WriteLine($"FINAL CHECK: Found existing User '{existingUser.Username}' (ID: {existingUser.UserID}) - updating instead of inserting");
                                                
                                                // Update instead of insert
                                                var existing = await GetEntityForUpdateAsync(onlineSet, onlineContext, existingUser.UserID);
                                                if (existing != null)
                                                {
                                                    await FixForeignKeys(localRecord, onlineContext);
                                                    onlineContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                    await UpdateEntityValues(existing, localRecord, onlineContext);
                                                    summary.Updated++;
                                                    result.TotalUpdated++;
                                                    continue; // Skip to next record
                                                }
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.WriteLine($"FINAL CHECK: No existing User found with username '{user.Username}' - proceeding with insert");
                                            }
                                        }
                                        catch (Exception finalCheckEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error in final User check: {finalCheckEx.Message}");
                                            // Continue with insert attempt - HandleUniqueKeyViolations will catch it
                                        }
                                    }
                                }
                                
                                var newRecord = CloneEntityForAdd(localRecord);
                                onlineSet.Add(newRecord);
                                summary.Uploaded++;
                                result.TotalUploaded++;
                                    
                                    // Log User records specifically for debugging
                                    if (typeof(T) == typeof(User))
                                    {
                                        var user = newRecord as User;
                                        if (user != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Adding User record to online: Username='{user.Username}' (ID: {GetId(newRecord)})");
                                        }
                                    }
                                    // Log Attendance records specifically for debugging
                                    if (typeof(T) == typeof(Attendance))
                                    {
                                        var attendance = newRecord as Attendance;
                                        if (attendance != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Adding Attendance record to online: MemberID={attendance.MemberID}, CheckinTime={attendance.CheckinTime}, CheckOutTime={attendance.CheckOutTime?.ToString() ?? "NULL"}");
                                        }
                                    }
                                    // Log Promotion records specifically for debugging
                                    if (typeof(T) == typeof(Promotion))
                                    {
                                        var promotion = newRecord as Promotion;
                                        if (promotion != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Adding Promotion record to online: PromoID={promotion.PromoID}, PromoName={promotion.PromoName}, DiscountRate={promotion.DiscountRate}");
                                        }
                                    }
                                    // Log Payment records specifically for debugging
                                    if (typeof(T) == typeof(Payment))
                                    {
                                        var payment = newRecord as Payment;
                                        if (payment != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Adding Payment record to online: PaymentID={GetId(newRecord)}, MemberID={payment.MemberID}, PaymentDate={payment.PaymentDate:yyyy-MM-dd HH:mm:ss}, Amount={payment.Amount}, PaymentType={payment.PaymentType}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error adding {tableName} record ID {id} to online: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        var onlineRecord = onlineDict[id];
                        var localUpdated = GetLastUpdated(localRecord);
                        var onlineUpdated = GetLastUpdated(onlineRecord);

                        if (localUpdated.HasValue && onlineUpdated.HasValue)
                        {
                            if (localUpdated.Value > onlineUpdated.Value)
                            {
                                // Only update if records are actually different
                                if (AreRecordsDifferent(localRecord, onlineRecord))
                                {
                                    if (typeof(T) == typeof(Member))
                                    {
                                        // Use direct update for Members to avoid MemberPromo tracking issues
                                        var member = localRecord as Member;
                                        if (member != null)
                                        {
                                            await FixForeignKeys(localRecord, onlineContext);
                                            if (await ValidateForeignKeys(localRecord, onlineContext) && await UpdateMemberDirectly(member, onlineContext, id))
                                            {
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                    if (existingOnline != null)
                                    {
                                        await FixForeignKeys(localRecord, onlineContext);
                                        if (await ValidateForeignKeys(localRecord, onlineContext))
                                        {
                                            onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                            await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                            summary.Updated++;
                                            result.TotalUpdated++;
                                        }
                                    }
                                    }
                                }
                            }
                            else if (onlineUpdated.Value > localUpdated.Value)
                            {
                                // Only update if records are actually different
                                if (AreRecordsDifferent(onlineRecord, localRecord))
                                {
                                    if (typeof(T) == typeof(Member))
                                    {
                                        // Use direct update for Members to avoid MemberPromo tracking issues
                                        var member = onlineRecord as Member;
                                        if (member != null)
                                        {
                                            await FixForeignKeys(onlineRecord, localContext);
                                            if (await ValidateForeignKeys(onlineRecord, localContext) && await UpdateMemberDirectly(member, localContext, id))
                                            {
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var existingLocal = await GetEntityForUpdateAsync(localSet, localContext, id);
                                    if (existingLocal != null)
                                    {
                                        await FixForeignKeys(onlineRecord, localContext);
                                        if (await ValidateForeignKeys(onlineRecord, localContext))
                                        {
                                            localContext.Entry(existingLocal).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                            await UpdateEntityValues(existingLocal, onlineRecord, localContext);
                                            summary.Updated++;
                                            result.TotalUpdated++;
                                        }
                                    }
                                    }
                                }
                            }
                            else if (localUpdated.Value == onlineUpdated.Value)
                            {
                                // Check if records are actually different before updating
                                if (AreRecordsDifferent(localRecord, onlineRecord))
                                {
                                    // Special handling for User entities with equal timestamps
                                    if (typeof(T) == typeof(User))
                                    {
                                        var localUser = localRecord as User;
                                        var onlineUser = onlineRecord as User;
                                        
                                        if (localUser != null && onlineUser != null)
                                        {
                                            // For User conflicts with equal timestamps, prefer the record with more complete data
                                            var localNonNullCount = CountNonNullFields(localUser);
                                            var onlineNonNullCount = CountNonNullFields(onlineUser);
                                            
                                            if (localNonNullCount >= onlineNonNullCount)
                                            {
                                                // Prefer local - update online with local data
                                                var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                                if (existingOnline != null)
                                        {
                                            await FixForeignKeys(localRecord, onlineContext);
                                                    if (await ValidateForeignKeys(localRecord, onlineContext))
                                                    {
                                                        onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                        await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                                        summary.Updated++; // Count as update, not conflict
                                                        result.TotalUpdated++;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // Prefer online - update local with online data
                                                var existingLocal = await GetEntityForUpdateAsync(localSet, localContext, id);
                                                if (existingLocal != null)
                                                {
                                                    await FixForeignKeys(onlineRecord, localContext);
                                                    if (await ValidateForeignKeys(onlineRecord, localContext))
                                                    {
                                                        localContext.Entry(existingLocal).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                        await UpdateEntityValues(existingLocal, onlineRecord, localContext);
                                                        summary.Updated++; // Count as update, not conflict
                                                        result.TotalUpdated++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if (typeof(T) == typeof(Member))
                                    {
                                        // Special handling for Member entities with equal timestamps
                                        var localMember = localRecord as Member;
                                        var onlineMember = onlineRecord as Member;
                                        
                                        if (localMember != null && onlineMember != null)
                                        {
                                            // For Member conflicts with equal timestamps, prefer the record with more complete data
                                            var localNonNullCount = CountNonNullFields(localMember);
                                            var onlineNonNullCount = CountNonNullFields(onlineMember);
                                            
                                            if (localNonNullCount >= onlineNonNullCount)
                                            {
                                                // Prefer local - update online with local data
                                                await FixForeignKeys(localRecord, onlineContext);
                                                if (await ValidateForeignKeys(localRecord, onlineContext) && await UpdateMemberDirectly(localMember, onlineContext, id))
                                            {
                                                    summary.Updated++; // Count as update, not conflict
                                                    result.TotalUpdated++;
                                                }
                                            }
                                            else
                                            {
                                                // Prefer online - update local with online data
                                                await FixForeignKeys(onlineRecord, localContext);
                                                if (await ValidateForeignKeys(onlineRecord, localContext) && await UpdateMemberDirectly(onlineMember, localContext, id))
                                                {
                                                    summary.Updated++; // Count as update, not conflict
                                                    result.TotalUpdated++;
                                                }
                                            }
                                        }
                                    }
                                    else if (typeof(T) == typeof(WalkIn))
                                    {
                                        // Special handling for WalkIn entities with equal timestamps
                                        var localWalkIn = localRecord as WalkIn;
                                        var onlineWalkIn = onlineRecord as WalkIn;
                                        
                                        if (localWalkIn != null && onlineWalkIn != null)
                                        {
                                            // For WalkIn conflicts with equal timestamps, prefer the record with more complete data
                                            var localNonNullCount = CountNonNullFields(localWalkIn);
                                            var onlineNonNullCount = CountNonNullFields(onlineWalkIn);
                                            
                                            if (localNonNullCount >= onlineNonNullCount)
                                            {
                                                // Prefer local - update online with local data
                                                var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                                if (existingOnline != null)
                                                {
                                                    await FixForeignKeys(localRecord, onlineContext);
                                                    if (await ValidateForeignKeys(localRecord, onlineContext))
                                                    {
                                                        onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                        await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                                        summary.Updated++; // Count as update, not conflict
                                                        result.TotalUpdated++;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // Prefer online - update local with online data
                                                var existingLocal = await GetEntityForUpdateAsync(localSet, localContext, id);
                                                if (existingLocal != null)
                                                {
                                                    await FixForeignKeys(onlineRecord, localContext);
                                                    if (await ValidateForeignKeys(onlineRecord, localContext))
                                                    {
                                                        localContext.Entry(existingLocal).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                        await UpdateEntityValues(existingLocal, onlineRecord, localContext);
                                                        summary.Updated++; // Count as update, not conflict
                                                        result.TotalUpdated++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                    if (existingOnline != null)
                                    {
                                        await FixForeignKeys(localRecord, onlineContext);
                                        if (await ValidateForeignKeys(localRecord, onlineContext))
                                        {
                                            onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                            await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                            summary.Conflicts++;
                                            result.TotalConflicts++;
                                        }
                                    }
                                    }
                                }
                                // If records are identical, skip update (no conflict to resolve)
                            }
                        }
                        else
                        {
                            // Check if records are actually different before updating
                            if (AreRecordsDifferent(localRecord, onlineRecord))
                            {
                                // Special handling for User entities with NULL timestamps
                                if (typeof(T) == typeof(User))
                                {
                                    var localUser = localRecord as User;
                                    var onlineUser = onlineRecord as User;
                                    
                                    if (localUser != null && onlineUser != null)
                                    {
                                        // For User conflicts with NULL timestamps, prefer the record with more complete data
                                        var localNonNullCount = CountNonNullFields(localUser);
                                        var onlineNonNullCount = CountNonNullFields(onlineUser);
                                        
                                        if (localNonNullCount >= onlineNonNullCount)
                                        {
                                            // Prefer local - update online with local data
                                            var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                            if (existingOnline != null)
                                    {
                                        await FixForeignKeys(localRecord, onlineContext);
                                                if (await ValidateForeignKeys(localRecord, onlineContext))
                                                {
                                                    onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                    await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                                    summary.Updated++; // Count as update, not conflict
                                                    result.TotalUpdated++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Prefer online - update local with online data
                                            var existingLocal = await GetEntityForUpdateAsync(localSet, localContext, id);
                                            if (existingLocal != null)
                                            {
                                                await FixForeignKeys(onlineRecord, localContext);
                                                if (await ValidateForeignKeys(onlineRecord, localContext))
                                                {
                                                    localContext.Entry(existingLocal).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                    await UpdateEntityValues(existingLocal, onlineRecord, localContext);
                                                    summary.Updated++; // Count as update, not conflict
                                                    result.TotalUpdated++;
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (typeof(T) == typeof(Member))
                                {
                                    // Special handling for Member entities with NULL timestamps
                                    var localMember = localRecord as Member;
                                    var onlineMember = onlineRecord as Member;
                                    
                                    if (localMember != null && onlineMember != null)
                                    {
                                        // For Member conflicts with NULL timestamps, prefer the record with more complete data
                                        var localNonNullCount = CountNonNullFields(localMember);
                                        var onlineNonNullCount = CountNonNullFields(onlineMember);
                                        
                                        if (localNonNullCount >= onlineNonNullCount)
                                        {
                                            // Prefer local - update online with local data
                                            await FixForeignKeys(localRecord, onlineContext);
                                            if (await ValidateForeignKeys(localRecord, onlineContext) && await UpdateMemberDirectly(localMember, onlineContext, id))
                                        {
                                                summary.Updated++; // Count as update, not conflict
                                                result.TotalUpdated++;
                                            }
                                        }
                                        else
                                        {
                                            // Prefer online - update local with online data
                                            await FixForeignKeys(onlineRecord, localContext);
                                            if (await ValidateForeignKeys(onlineRecord, localContext) && await UpdateMemberDirectly(onlineMember, localContext, id))
                                            {
                                                summary.Updated++; // Count as update, not conflict
                                                result.TotalUpdated++;
                                            }
                                        }
                                    }
                                }
                                else if (typeof(T) == typeof(WalkIn))
                                {
                                    // Special handling for WalkIn entities with NULL timestamps
                                    var localWalkIn = localRecord as WalkIn;
                                    var onlineWalkIn = onlineRecord as WalkIn;
                                    
                                    if (localWalkIn != null && onlineWalkIn != null)
                                    {
                                        // For WalkIn conflicts with NULL timestamps, prefer the record with more complete data
                                        var localNonNullCount = CountNonNullFields(localWalkIn);
                                        var onlineNonNullCount = CountNonNullFields(onlineWalkIn);
                                        
                                        if (localNonNullCount >= onlineNonNullCount)
                                        {
                                            // Prefer local - update online with local data
                                            var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                            if (existingOnline != null)
                                            {
                                                await FixForeignKeys(localRecord, onlineContext);
                                                if (await ValidateForeignKeys(localRecord, onlineContext))
                                                {
                                                    onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                    await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                                    summary.Updated++; // Count as update, not conflict
                                                    result.TotalUpdated++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Prefer online - update local with online data
                                            var existingLocal = await GetEntityForUpdateAsync(localSet, localContext, id);
                                            if (existingLocal != null)
                                            {
                                                await FixForeignKeys(onlineRecord, localContext);
                                                if (await ValidateForeignKeys(onlineRecord, localContext))
                                                {
                                                    localContext.Entry(existingLocal).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                    await UpdateEntityValues(existingLocal, onlineRecord, localContext);
                                                    summary.Updated++; // Count as update, not conflict
                                                    result.TotalUpdated++;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var existingOnline = await GetEntityForUpdateAsync(onlineSet, onlineContext, id);
                                if (existingOnline != null)
                                {
                                    await FixForeignKeys(localRecord, onlineContext);
                                    if (await ValidateForeignKeys(localRecord, onlineContext))
                                    {
                                        onlineContext.Entry(existingOnline).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                        await UpdateEntityValues(existingOnline, localRecord, onlineContext);
                                        summary.Conflicts++;
                                        result.TotalConflicts++;
                                    }
                                }
                                }
                            }
                        }
                    }
                }

                // Download missing online data to local
                foreach (var onlineRecord in onlineRecords)
                {
                    var id = GetId(onlineRecord);
                    
                    if (id <= 0)
                    {
                        continue;
                    }
                    
                    // Fix any invalid foreign key references BEFORE validation
                    await FixForeignKeys(onlineRecord, localContext);
                    
                    if (!await ValidateForeignKeys(onlineRecord, localContext))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping {tableName} record ID {id} - foreign key validation failed");
                        continue;
                    }
                    
                    var existingByUniqueKey = await FindByUniqueKey<T>(onlineRecord, localContext);
                    
                    if (!localDict.ContainsKey(id))
                    {
                        if (existingByUniqueKey != null)
                        {
                            try
                            {
                                var existingId = GetId(existingByUniqueKey);
                                if (typeof(T) == typeof(Member))
                                {
                                    // Use direct update for Members to avoid MemberPromo tracking issues
                                    var member = onlineRecord as Member;
                                    if (member != null)
                                    {
                                        await FixForeignKeys(onlineRecord, localContext);
                                        if (await UpdateMemberDirectly(member, localContext, existingId))
                                        {
                                            summary.Updated++;
                                            result.TotalUpdated++;
                                        }
                                    }
                                }
                                else
                                {
                                    var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                if (existing != null)
                                {
                                    localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                        await UpdateEntityValues(existing, onlineRecord, localContext);
                                    summary.Updated++;
                                    result.TotalUpdated++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating {tableName} by unique key: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Record exists only in online - download to local
                            // BEFORE adding, check for unique key conflicts
                            if (typeof(T) == typeof(User))
                            {
                                var user = onlineRecord as User;
                                if (user != null && !string.IsNullOrEmpty(user.Username))
                                {
                                    // Check if a user with this username already exists in local database
                                    var existingUser = await FindByUniqueKeyValue(user.Username, localContext);
                                    
                                    // If not found, try with trimmed and normalized versions
                                    if (existingUser == null)
                                    {
                                        var trimmedUsername = user.Username.Trim();
                                        if (trimmedUsername != user.Username)
                                        {
                                            existingUser = await FindByUniqueKeyValue(trimmedUsername, localContext);
                                        }
                                    }
                                    
                                    if (existingUser != null)
                                    {
                                        // User exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingUser);
                                            var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                            if (existing != null)
                                            {
                                                await FixForeignKeys(onlineRecord, localContext);
                                                localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                await UpdateEntityValues(existing, onlineRecord, localContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing User '{user.Username}' (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating User by unique key in pre-validation: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Trainer))
                            {
                                var trainer = onlineRecord as Trainer;
                                if (trainer != null)
                                {
                                    // Check if a trainer with same name/contact already exists in local database
                                    var existingTrainer = await FindTrainerByUniqueFields(trainer, localContext);
                                    if (existingTrainer != null)
                                    {
                                        // Trainer exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingTrainer);
                                            var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                            if (existing != null)
                                            {
                                                localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                UpdateEntityValues(existing, onlineRecord, localContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Trainer '{trainer.FirstName} {trainer.LastName}' (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Trainer by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Member))
                            {
                                var member = onlineRecord as Member;
                                if (member != null)
                                {
                                    // Check if a member with same email/contact already exists in local database
                                    var existingMember = await FindMemberByUniqueFields(member, localContext);
                                    if (existingMember != null)
                                    {
                                        // Member exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingMember);
                                            // Use direct update for Members to avoid MemberPromo tracking issues
                                            await FixForeignKeys(onlineRecord, localContext);
                                            if (await UpdateMemberDirectly(member, localContext, existingId))
                                            {
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Member '{member.FirstName} {member.LastName}' (Email: {member.Email}) (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Member by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Promotion))
                            {
                                var promotion = onlineRecord as Promotion;
                                if (promotion != null)
                                {
                                    // Check if a promotion with same name, dates, and discount already exists in local database
                                    var existingPromotion = await FindPromotionByUniqueFields(promotion, localContext);
                                    if (existingPromotion != null)
                                    {
                                        // Promotion exists - update it instead of inserting
                                        try
                                        {
                                            var existingId = GetId(existingPromotion);
                                            var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                            if (existing != null)
                                            {
                                                await FixForeignKeys(onlineRecord, localContext);
                                                localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                await UpdateEntityValues(existing, onlineRecord, localContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                System.Diagnostics.Debug.WriteLine($"Pre-validated: Updated existing Promotion '{promotion.PromoName}' (ID: {existingId}) instead of inserting");
                                                continue; // Skip to next record
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Promotion by unique fields in pre-validation: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"No matching Promotion found in local for online Promotion ID {GetId(promotion)}: '{promotion.PromoName}' - will insert as new");
                                    }
                                }
                            }
                            
                            // If no unique key conflict found, proceed with insert
                            try
                            {
                                if (typeof(T) == typeof(Member))
                                {
                                    // Use direct insert for Members to avoid MemberPromo tracking issues
                                    var member = onlineRecord as Member;
                                    if (member != null)
                                    {
                                        await FixForeignKeys(onlineRecord, localContext);
                                        if (await InsertMemberDirectly(member, localContext))
                                        {
                                            summary.Downloaded++;
                                            result.TotalDownloaded++;
                                        }
                                    }
                                }
                                else
                            {
                                var newRecord = CloneEntityForAdd(onlineRecord);
                                localSet.Add(newRecord);
                                summary.Downloaded++;
                                result.TotalDownloaded++;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error adding {tableName} record ID {id} to local: {ex.Message}");
                            }
                        }
                    }
                }

                // Save changes for this table with unique key violation handling
                // Skip SaveChanges for Members since we use direct SQL/ExecuteUpdate (no change tracker)
                if (typeof(T) != typeof(Member))
                {
                    // For User entities, wrap in try-catch to prevent sync failure
                    if (typeof(T) == typeof(User))
                    {
                        try
                        {
                            await SaveChangesWithUniqueKeyHandling(localContext, localSet, summary, result, tableName, "local");
                        }
                        catch (Exception userLocalEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠ User sync failed for local database: {userLocalEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Continuing sync for other tables...");
                            // Don't throw - allow other tables to sync
                        }
                        
                        try
                        {
                            await SaveChangesWithUniqueKeyHandling(onlineContext, onlineSet, summary, result, tableName, "online");
                        }
                        catch (Exception userOnlineEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠ User sync failed for online database: {userOnlineEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Continuing sync for other tables...");
                            // Don't throw - allow other tables to sync
                        }
                    }
                    else
                    {
                        await SaveChangesWithUniqueKeyHandling(localContext, localSet, summary, result, tableName, "local");
                        await SaveChangesWithUniqueKeyHandling(onlineContext, onlineSet, summary, result, tableName, "online");
                    }
                }
                else
                {
                    // For Members, we've already executed all operations directly (ExecuteUpdate/InsertMemberDirectly)
                    // No SaveChanges needed, but we should still clear any tracked entities
                    DetachAllCompositeKeyEntities(localContext);
                    DetachAllCompositeKeyEntities(onlineContext);
                    System.Diagnostics.Debug.WriteLine("Skipped SaveChanges for Members (using direct SQL operations)");
                }

                // CRITICAL: Clear the change tracker completely after each table sync
                // This prevents MemberPromo and other entities from persisting between table syncs
                localContext.ChangeTracker.Clear();
                onlineContext.ChangeTracker.Clear();
                System.Diagnostics.Debug.WriteLine($"Cleared change tracker after {tableName} sync");

                result.TableSummaries[tableName] = summary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing table {typeof(T).Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task SyncTableOnlineToLocal<T>(AppDbContext localContext, AppDbContext onlineContext, SyncResult result) where T : class
        {
            try
            {
                var tableName = typeof(T).Name;
                var summary = new TableSyncSummary { TableName = tableName };

                // Clear the change tracker before starting
                localContext.ChangeTracker.Clear();
                onlineContext.ChangeTracker.Clear();
                System.Diagnostics.Debug.WriteLine($"Cleared change tracker before starting {tableName} download");

                // Detach all composite key entities
                DetachAllCompositeKeyEntities(localContext);
                DetachAllCompositeKeyEntities(onlineContext);

                var localSet = localContext.Set<T>();
                var onlineSet = onlineContext.Set<T>();

                var localRecords = await localSet.AsNoTracking().ToListAsync();
                var onlineRecords = await onlineSet.AsNoTracking().ToListAsync();

                // Group by ID to handle duplicates
                var localDict = localRecords
                    .GroupBy(r => GetId(r))
                    .Where(g => g.Key > 0)
                    .ToDictionary(g => g.Key, g => g.First());

                var onlineDict = onlineRecords
                    .GroupBy(r => GetId(r))
                    .Where(g => g.Key > 0)
                    .ToDictionary(g => g.Key, g => g.First());

                // Download missing online data to local
                foreach (var onlineRecord in onlineRecords)
                {
                    var id = GetId(onlineRecord);
                    
                    if (id <= 0)
                    {
                        continue;
                    }
                    
                    // Fix any invalid foreign key references BEFORE validation
                    await FixForeignKeys(onlineRecord, localContext);
                    
                    if (!await ValidateForeignKeys(onlineRecord, localContext))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping {tableName} record ID {id} - foreign key validation failed");
                        continue;
                    }
                    
                    var existingByUniqueKey = await FindByUniqueKey<T>(onlineRecord, localContext);
                    
                    if (!localDict.ContainsKey(id))
                    {
                        if (existingByUniqueKey != null)
                        {
                            try
                            {
                                var existingId = GetId(existingByUniqueKey);
                                if (typeof(T) == typeof(Member))
                                {
                                    var member = onlineRecord as Member;
                                    if (member != null)
                                    {
                                        await FixForeignKeys(onlineRecord, localContext);
                                        if (await UpdateMemberDirectly(member, localContext, existingId))
                                        {
                                            summary.Updated++;
                                            result.TotalUpdated++;
                                        }
                                    }
                                }
                                else
                                {
                                    var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                    if (existing != null)
                                    {
                                        localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                        await UpdateEntityValues(existing, onlineRecord, localContext);
                                        summary.Updated++;
                                        result.TotalUpdated++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating {tableName} by unique key: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Record exists only in online - download to local
                            // Check for unique key conflicts
                            if (typeof(T) == typeof(User))
                            {
                                var user = onlineRecord as User;
                                if (user != null && !string.IsNullOrEmpty(user.Username))
                                {
                                    var existingUser = await FindByUniqueKeyValue(user.Username, localContext);
                                    if (existingUser != null)
                                    {
                                        try
                                        {
                                            var existingId = GetId(existingUser);
                                            var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                            if (existing != null)
                                            {
                                                await FixForeignKeys(onlineRecord, localContext);
                                                localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                await UpdateEntityValues(existing, onlineRecord, localContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                continue;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating User by unique key: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Trainer))
                            {
                                var trainer = onlineRecord as Trainer;
                                if (trainer != null)
                                {
                                    var existingTrainer = await FindTrainerByUniqueFields(trainer, localContext);
                                    if (existingTrainer != null)
                                    {
                                        try
                                        {
                                            var existingId = GetId(existingTrainer);
                                            var existing = await GetEntityForUpdateAsync(localSet, localContext, existingId);
                                            if (existing != null)
                                            {
                                                localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                UpdateEntityValues(existing, onlineRecord, localContext);
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                continue;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Trainer by unique fields: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else if (typeof(T) == typeof(Member))
                            {
                                var member = onlineRecord as Member;
                                if (member != null)
                                {
                                    var existingMember = await FindMemberByUniqueFields(member, localContext);
                                    if (existingMember != null)
                                    {
                                        try
                                        {
                                            var existingId = GetId(existingMember);
                                            await FixForeignKeys(onlineRecord, localContext);
                                            if (await UpdateMemberDirectly(member, localContext, existingId))
                                            {
                                                summary.Updated++;
                                                result.TotalUpdated++;
                                                continue;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating Member by unique fields: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            
                            // If no unique key conflict found, proceed with insert
                            try
                            {
                                if (typeof(T) == typeof(Member))
                                {
                                    var member = onlineRecord as Member;
                                    if (member != null)
                                    {
                                        await FixForeignKeys(onlineRecord, localContext);
                                        if (await InsertMemberDirectly(member, localContext))
                                        {
                                            summary.Downloaded++;
                                            result.TotalDownloaded++;
                                        }
                                    }
                                }
                                else
                                {
                                    var newRecord = CloneEntityForAdd(onlineRecord);
                                    localSet.Add(newRecord);
                                    summary.Downloaded++;
                                    result.TotalDownloaded++;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error adding {tableName} record ID {id} to local: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Record exists in both - update local with online data (one-way sync)
                        try
                        {
                            if (typeof(T) == typeof(Member))
                            {
                                var member = onlineRecord as Member;
                                if (member != null)
                                {
                                    await FixForeignKeys(onlineRecord, localContext);
                                    if (await UpdateMemberDirectly(member, localContext, id))
                                    {
                                        summary.Updated++;
                                        result.TotalUpdated++;
                                    }
                                }
                            }
                            else
                            {
                                var existing = await GetEntityForUpdateAsync(localSet, localContext, id);
                                if (existing != null)
                                {
                                    await FixForeignKeys(onlineRecord, localContext);
                                    if (await ValidateForeignKeys(onlineRecord, localContext))
                                    {
                                        localContext.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                        await UpdateEntityValues(existing, onlineRecord, localContext);
                                        summary.Updated++;
                                        result.TotalUpdated++;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error updating {tableName} record ID {id}: {ex.Message}");
                        }
                    }
                }

                // Save changes for this table
                if (typeof(T) != typeof(Member))
                {
                    if (typeof(T) == typeof(User))
                    {
                        try
                        {
                            await SaveChangesWithUniqueKeyHandling(localContext, localSet, summary, result, tableName, "local");
                        }
                        catch (Exception userLocalEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠ User sync failed for local database: {userLocalEx.Message}");
                        }
                    }
                    else
                    {
                        await SaveChangesWithUniqueKeyHandling(localContext, localSet, summary, result, tableName, "local");
                    }
                }
                else
                {
                    DetachAllCompositeKeyEntities(localContext);
                    System.Diagnostics.Debug.WriteLine("Skipped SaveChanges for Members (using direct SQL operations)");
                }

                // Clear the change tracker after sync
                localContext.ChangeTracker.Clear();
                onlineContext.ChangeTracker.Clear();
                System.Diagnostics.Debug.WriteLine($"Cleared change tracker after {tableName} download");

                result.TableSummaries[tableName] = summary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading table {typeof(T).Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task SaveChangesWithUniqueKeyHandling<T>(
            AppDbContext context, 
            DbSet<T> dbSet, 
            TableSyncSummary summary, 
            SyncResult result, 
            string tableName,
            string dbType) where T : class
        {
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    // IMMEDIATE CHECK: If we're not syncing MemberPromo, there should be NO MemberPromo entities tracked
                    // If there are, something went wrong - clear them immediately
                    if (typeof(T).Name != "MemberPromo")
                    {
                        var memberPromoEntries = context.ChangeTracker.Entries()
                            .Where(e => e.Entity.GetType().Name == "MemberPromo" || 
                                       e.Entity.GetType().Name == "MemberTrainer")
                            .ToList();
                        
                        if (memberPromoEntries.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"CRITICAL: Found {memberPromoEntries.Count} MemberPromo/MemberTrainer entities tracked during {tableName} sync! Clearing immediately...");
                            foreach (var entry in memberPromoEntries)
                            {
                                entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            }
                        }
                    }
                    
                    // Only detach MemberPromo and MemberTrainer entities, keep all other entities
                    // This ensures Attendance and other records are not lost
                    var allEntries = context.ChangeTracker.Entries().ToList();
                    var entitiesToDetach = allEntries
                        .Where(e => e.Entity.GetType().Name == "MemberPromo" || 
                                   e.Entity.GetType().Name == "MemberTrainer")
                        .ToList();
                    
                    foreach (var entry in entitiesToDetach)
                    {
                        System.Diagnostics.Debug.WriteLine($"Detaching {entry.Entity.GetType().Name} entity before SaveChanges for {tableName}");
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    
                    // Log what entities are being saved
                    var entitiesBeingSaved = context.ChangeTracker.Entries()
                        .Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        .Select(e => $"{e.Entity.GetType().Name} ({e.State})")
                        .ToList();
                    
                    // Count Attendance records specifically
                    var attendanceCount = context.ChangeTracker.Entries<Attendance>()
                        .Count(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Detached);
                    
                    if (entitiesBeingSaved.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Saving {entitiesBeingSaved.Count} entities for {tableName}: {string.Join(", ", entitiesBeingSaved)}");
                        if (attendanceCount > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"  -> Including {attendanceCount} Attendance record(s)");
                        }
                    }
                    
                    var saveResult = await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"SaveChanges for {tableName} ({dbType}) completed. Rows affected: {saveResult}");
                    return; // Success
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                    
                    // Check for MemberPromo composite key error (can happen for ANY table, not just Members)
                    if (innerMsg.Contains("MemberPromo.MemberID") && innerMsg.Contains("part of a key"))
                    {
                        System.Diagnostics.Debug.WriteLine($"MemberPromo composite key error detected during {tableName} sync. This should not happen with the new approach, but retrying...");
                        
                        // Get all entities we want to keep (the ones we're actually trying to save)
                        var entitiesToKeep = context.ChangeTracker.Entries()
                            .Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Detached && 
                                       e.Entity.GetType().Name != "MemberPromo" &&
                                       e.Entity.GetType().Name != "MemberTrainer")
                            .Select(e => new { Entity = e.Entity, State = e.State, Type = e.Entity.GetType() })
                            .ToList();
                        
                        // Clear the change tracker completely
                        context.ChangeTracker.Clear();
                        
                        // Re-add only the entities we want to keep
                        foreach (var item in entitiesToKeep)
                        {
                            try
                            {
                                // Use reflection to call Set<T>() with the runtime type
                                var setMethod = typeof(Microsoft.EntityFrameworkCore.DbContext).GetMethod("Set", Type.EmptyTypes);
                                var genericSetMethod = setMethod.MakeGenericMethod(item.Type);
                                var entityDbSet = genericSetMethod.Invoke(context, null);
                                
                                // Get the appropriate method based on state
                                if (item.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                                {
                                    var addMethod = entityDbSet.GetType().GetMethod("Add");
                                    addMethod.Invoke(entityDbSet, new[] { item.Entity });
                                }
                                else if (item.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
                                {
                                    var updateMethod = entityDbSet.GetType().GetMethod("Update");
                                    updateMethod.Invoke(entityDbSet, new[] { item.Entity });
                                }
                                else if (item.State == Microsoft.EntityFrameworkCore.EntityState.Deleted)
                                {
                                    var removeMethod = entityDbSet.GetType().GetMethod("Remove");
                                    removeMethod.Invoke(entityDbSet, new[] { item.Entity });
                                }
                            }
                            catch (Exception reAddEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error re-adding {item.Type.Name} entity: {reAddEx.Message}");
                            }
                        }
                        
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            System.Diagnostics.Debug.WriteLine($"Cleared change tracker and re-added entities, retrying SaveChanges for {tableName} (attempt {retryCount}/{maxRetries})");
                            continue; // Retry
                        }
                        else
                        {
                            throw new Exception($"Error saving {dbType} changes for {tableName}: MemberPromo composite key error could not be resolved after {maxRetries} attempts. {innerMsg}", dbEx);
                        }
                    }
                    else if (innerMsg.Contains("UNIQUE KEY") || innerMsg.Contains("duplicate key"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Unique key violation detected for {tableName} in {dbType} database. Error message: {innerMsg}");
                        System.Diagnostics.Debug.WriteLine($"Attempting to resolve... (Retry attempt {retryCount + 1}/{maxRetries})");
                        
                        // Extract the conflicting value from the error message if possible
                        string? conflictingValue = ExtractConflictingValueFromError(innerMsg);
                        if (!string.IsNullOrEmpty(conflictingValue))
                        {
                            System.Diagnostics.Debug.WriteLine($"Extracted conflicting value from error: '{conflictingValue}'");
                        }
                        
                        // For User entities, if this is the last retry, be ultra-aggressive
                        if (typeof(T) == typeof(User) && retryCount >= maxRetries - 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠ LAST RETRY ATTEMPT: Removing ALL added User entries to prevent sync failure");
                            var allAddedUsers = context.ChangeTracker.Entries<User>()
                                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                                .ToList();
                            
                            if (allAddedUsers.Any())
                            {
                                foreach (var entry in allAddedUsers)
                                {
                                    System.Diagnostics.Debug.WriteLine($"  Removing User: '{entry.Entity.Username}' (ID: {GetId(entry.Entity)})");
                                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                }
                                
                                // Also clear any Modified User entries that might be causing issues
                                var allModifiedUsers = context.ChangeTracker.Entries<User>()
                                    .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
                                    .ToList();
                                
                                if (allModifiedUsers.Any())
                                {
                                    System.Diagnostics.Debug.WriteLine($"  Also detaching {allModifiedUsers.Count} Modified User entries");
                                    foreach (var entry in allModifiedUsers)
                                    {
                                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                    }
                                }
                                
                                retryCount++;
                                System.Diagnostics.Debug.WriteLine($"Removed all User entries, retrying SaveChanges (final attempt {retryCount}/{maxRetries})");
                                continue; // Retry one more time
                            }
                        }
                        
                        // For User entities, try a direct database query first before HandleUniqueKeyViolations
                        if (typeof(T) == typeof(User) && !string.IsNullOrEmpty(conflictingValue))
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"=== UNIQUE KEY VIOLATION FOR USER: '{conflictingValue}' ===");
                                
                                // First, check if the user is already tracked in the change tracker (in any state)
                                var conflictLower = conflictingValue.Trim().ToLower();
                                var allTrackedUsers = context.ChangeTracker.Entries<User>().ToList();
                                System.Diagnostics.Debug.WriteLine($"All tracked User entries: {allTrackedUsers.Count}");
                                
                                bool foundTrackedUser = false;
                                foreach (var trackedEntry in allTrackedUsers)
                                {
                                    var trackedUser = trackedEntry.Entity;
                                    var trackedUsernameLower = trackedUser.Username?.Trim().ToLower() ?? "";
                                    System.Diagnostics.Debug.WriteLine($"  - Tracked User: Username='{trackedUser.Username}' (ID: {GetId(trackedUser)}, State: {trackedEntry.State})");
                                    
                                    if (trackedUsernameLower == conflictLower && trackedEntry.State != Microsoft.EntityFrameworkCore.EntityState.Added)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"✓ Found User already tracked in state {trackedEntry.State} - detaching added entry");
                                        
                                        // Find and detach the added entry
                                        var addedEntries = context.ChangeTracker.Entries<User>()
                                            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added &&
                                                      (e.Entity.Username?.Trim().ToLower() == conflictLower))
                                            .ToList();
                                        
                                        foreach (var addedEntry in addedEntries)
                                        {
                                            addedEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                            summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                                            summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                                        }
                                        
                                        foundTrackedUser = true;
                                        break;
                                    }
                                }
                                
                                if (foundTrackedUser)
                                {
                                    retryCount++;
                                    System.Diagnostics.Debug.WriteLine($"Removed duplicate added User entry, retrying SaveChanges (attempt {retryCount}/{maxRetries})");
                                    continue; // Retry SaveChanges
                                }
                                
                                // First, list ALL users in the database to see what we're working with
                                var allUsersInDb = await context.Users.AsNoTracking().ToListAsync();
                                System.Diagnostics.Debug.WriteLine($"Total users in {dbType} database: {allUsersInDb.Count}");
                                foreach (var u in allUsersInDb.Take(20))
                                {
                                    System.Diagnostics.Debug.WriteLine($"  - User ID {u.UserID}: Username='{u.Username}' (lower: '{u.Username?.Trim().ToLower()}')");
                                }
                                
                                // List all added User entries in change tracker
                                var addedUserEntries = context.ChangeTracker.Entries<User>()
                                    .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                                    .ToList();
                                System.Diagnostics.Debug.WriteLine($"Added User entries in change tracker: {addedUserEntries.Count}");
                                foreach (var entry in addedUserEntries)
                                {
                                    var user = entry.Entity;
                                    System.Diagnostics.Debug.WriteLine($"  - Added User: Username='{user.Username}' (ID: {GetId(user)}, State: {entry.State})");
                                }
                                
                                // Try multiple variations of the username
                                var searchTerms = new List<string> { conflictingValue, conflictingValue.Trim() };
                                if (conflictingValue.Contains("."))
                                {
                                    searchTerms.Add(conflictingValue.Replace(".", ""));
                                    searchTerms.Add(conflictingValue.Replace(".", " "));
                                }
                                
                                System.Diagnostics.Debug.WriteLine($"Searching for User with terms: {string.Join(", ", searchTerms.Select(t => $"'{t}'"))}");
                                
                                foreach (var searchTerm in searchTerms.Distinct())
                                {
                                    if (string.IsNullOrEmpty(searchTerm))
                                        continue;
                                        
                                    var sql = "SELECT * FROM Users WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@p0)))";
                                    var existingUsers = await context.Users
                                        .FromSqlRaw(sql, new Microsoft.Data.SqlClient.SqlParameter("@p0", searchTerm))
                                        .AsNoTracking()
                                        .ToListAsync();
                                    
                                    if (existingUsers.Any())
                                    {
                                        var existingUser = existingUsers.First();
                                        System.Diagnostics.Debug.WriteLine($"✓ Found existing User via direct SQL query: '{existingUser.Username}' (ID: {existingUser.UserID})");
                                        
                                        // Find the added entry and convert it to an update
                                        var addedEntries = context.ChangeTracker.Entries<User>()
                                            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                                            .ToList();
                                        
                                        foreach (var addedEntry in addedEntries)
                                        {
                                            var addedUser = addedEntry.Entity;
                                            var addedUsernameLower = addedUser.Username?.Trim().ToLower() ?? "";
                                            var conflictLowerLocal = conflictingValue.Trim().ToLower();
                                            var existingUsernameLower = existingUser.Username?.Trim().ToLower() ?? "";
                                            
                                            if (addedUsernameLower == conflictLowerLocal ||
                                                addedUsernameLower == existingUsernameLower ||
                                                addedUsernameLower.Replace(".", "") == conflictLowerLocal.Replace(".", "") ||
                                                addedUsernameLower.Replace(".", "") == existingUsernameLower.Replace(".", ""))
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Converting added User '{addedUser.Username}' (ID: {GetId(addedUser)}) to update for existing User ID {existingUser.UserID}");
                                                
                                                // Detach the added entity
                                                addedEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                                
                                                // Get the existing entity and update it
                                                var existing = await GetEntityForUpdateAsync(dbSet as DbSet<User>, context, existingUser.UserID);
                                                if (existing != null)
                                                {
                                                    await FixForeignKeys(addedUser, context);
                                                    context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                    await UpdateEntityValues(existing, addedUser, context);
                                                    summary.Updated++;
                                                    result.TotalUpdated++;
                                                    summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                                                    summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                                                    System.Diagnostics.Debug.WriteLine($"✓ Successfully converted User insert to update");
                                                }
                                                else
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"✗ Could not load existing User entity for ID {existingUser.UserID}");
                                                }
                                            }
                                        }
                                        
                                        retryCount++;
                                        System.Diagnostics.Debug.WriteLine($"Resolved unique key violation via direct query, retrying SaveChanges (attempt {retryCount}/{maxRetries})");
                                        continue; // Retry SaveChanges
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"✗ No User found with search term: '{searchTerm}'");
                                    }
                                }
                                
                                // If still not found, try matching against ALL users in database (case-insensitive, normalized)
                                bool aggressiveMatchFound = false;
                                if (addedUserEntries.Any() && !aggressiveMatchFound)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Trying aggressive matching against all users in database...");
                                    var conflictLowerAggressive = conflictingValue.Trim().ToLower();
                                    var conflictNormalized = System.Text.RegularExpressions.Regex.Replace(conflictLowerAggressive, @"[^a-zA-Z0-9]", "");
                                    
                                    foreach (var dbUser in allUsersInDb)
                                    {
                                        if (aggressiveMatchFound) break;
                                        
                                        var dbUsernameLower = dbUser.Username?.Trim().ToLower() ?? "";
                                        var dbUsernameNormalized = System.Text.RegularExpressions.Regex.Replace(dbUsernameLower, @"[^a-zA-Z0-9]", "");
                                        
                                        if (dbUsernameLower == conflictLowerAggressive || 
                                            dbUsernameNormalized == conflictNormalized ||
                                            (conflictLowerAggressive.Contains(".") && dbUsernameLower == conflictLowerAggressive.Replace(".", "")) ||
                                            (conflictLowerAggressive.Contains(".") && dbUsernameLower == conflictLowerAggressive.Replace(".", " ")))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"✓ Found matching User via aggressive search: '{dbUser.Username}' (ID: {dbUser.UserID})");
                                            
                                            // Find and convert the added entry
                                            foreach (var addedEntry in addedUserEntries)
                                            {
                                                if (aggressiveMatchFound) break;
                                                
                                                var addedUser = addedEntry.Entity;
                                                var addedUsernameLower = addedUser.Username?.Trim().ToLower() ?? "";
                                                var addedUsernameNormalized = System.Text.RegularExpressions.Regex.Replace(addedUsernameLower, @"[^a-zA-Z0-9]", "");
                                                
                                                if (addedUsernameLower == conflictLowerAggressive ||
                                                    addedUsernameNormalized == conflictNormalized ||
                                                    addedUsernameLower == dbUsernameLower ||
                                                    addedUsernameNormalized == dbUsernameNormalized)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"Converting added User '{addedUser.Username}' to update for existing User ID {dbUser.UserID}");
                                                    
                                                    // Detach the added entity
                                                    addedEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                                    
                                                    // Get the existing entity and update it
                                                    var existing = await GetEntityForUpdateAsync(dbSet as DbSet<User>, context, dbUser.UserID);
                                                    if (existing != null)
                                                    {
                                                        await FixForeignKeys(addedUser, context);
                                                        context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                                        await UpdateEntityValues(existing, addedUser, context);
                                                        summary.Updated++;
                                                        result.TotalUpdated++;
                                                        summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                                                        summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                                                        System.Diagnostics.Debug.WriteLine($"✓ Successfully converted User insert to update via aggressive matching");
                                                        aggressiveMatchFound = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    if (aggressiveMatchFound)
                                    {
                                        retryCount++;
                                        System.Diagnostics.Debug.WriteLine($"Resolved unique key violation via aggressive matching, retrying SaveChanges (attempt {retryCount}/{maxRetries})");
                                        continue; // This will continue the while loop, retrying SaveChanges
                                    }
                                }
                                
                                System.Diagnostics.Debug.WriteLine($"=== END USER UNIQUE KEY VIOLATION HANDLING ===");
                            }
                            catch (Exception directQueryEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in direct database query for User: {directQueryEx.Message}");
                                System.Diagnostics.Debug.WriteLine($"Stack trace: {directQueryEx.StackTrace}");
                            }
                        }
                        
                        if (await HandleUniqueKeyViolations<T>(context, dbSet, summary, result, tableName, conflictingValue))
                        {
                            retryCount++;
                            System.Diagnostics.Debug.WriteLine($"Resolved unique key violations, retrying SaveChanges (attempt {retryCount}/{maxRetries})");
                            continue; // Retry
                        }
                        else
                        {
                            // Last resort: For User entities, try to remove the duplicate and log a warning
                            if (typeof(T) == typeof(User) && !string.IsNullOrEmpty(conflictingValue))
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"=== LAST RESORT: Attempting to remove duplicate User entry ===");
                                    
                                    var conflictLower = conflictingValue.Trim().ToLower();
                                    var conflictNormalized = System.Text.RegularExpressions.Regex.Replace(conflictLower, @"[^a-zA-Z0-9]", "");
                                    
                                    // Get all added User entries and try to match them
                                    var allAddedUsers = context.ChangeTracker.Entries<User>()
                                        .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                                        .ToList();
                                    
                                    System.Diagnostics.Debug.WriteLine($"Found {allAddedUsers.Count} added User entries to check");
                                    
                                    var usersToRemove = new List<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<User>>();
                                    
                                    foreach (var entry in allAddedUsers)
                                    {
                                        var user = entry.Entity;
                                        var usernameLower = user.Username?.Trim().ToLower() ?? "";
                                        var usernameNormalized = System.Text.RegularExpressions.Regex.Replace(usernameLower, @"[^a-zA-Z0-9]", "");
                                        
                                        System.Diagnostics.Debug.WriteLine($"  Checking User: '{user.Username}' (lower: '{usernameLower}', normalized: '{usernameNormalized}')");
                                        
                                        // Match if username matches (exact, normalized, or contains)
                                        if (usernameLower == conflictLower ||
                                            usernameNormalized == conflictNormalized ||
                                            usernameLower.Contains(conflictLower) ||
                                            conflictLower.Contains(usernameLower))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"  ✓ MATCH FOUND - Will remove duplicate User: '{user.Username}' (ID: {GetId(user)})");
                                            usersToRemove.Add(entry);
                                        }
                                    }
                                    
                                    if (usersToRemove.Any())
                                    {
                                        foreach (var entry in usersToRemove)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Removing duplicate User entry: '{entry.Entity.Username}' (ID: {GetId(entry.Entity)})");
                                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                            summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                                            summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                                        }
                                        
                                        retryCount++;
                                        System.Diagnostics.Debug.WriteLine($"✓ Removed {usersToRemove.Count} duplicate User entries, retrying SaveChanges (attempt {retryCount}/{maxRetries})");
                                        continue; // Retry
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"✗ No matching User entries found to remove - trying ultra-aggressive removal");
                                        
                                        // Ultra-aggressive: Remove ALL added User entries if we can't find a match
                                        // This prevents the sync from failing completely
                                        var allAdded = context.ChangeTracker.Entries<User>()
                                            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                                            .ToList();
                                        
                                        if (allAdded.Any())
                                        {
                                            System.Diagnostics.Debug.WriteLine($"⚠ ULTRA-AGGRESSIVE: Removing ALL {allAdded.Count} added User entries to prevent sync failure");
                                            foreach (var entry in allAdded)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"  Removing: '{entry.Entity.Username}' (ID: {GetId(entry.Entity)})");
                                                entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                                summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                                                summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                                            }
                                            
                                            retryCount++;
                                            System.Diagnostics.Debug.WriteLine($"✓ Removed all added User entries, retrying SaveChanges (attempt {retryCount}/{maxRetries})");
                                            continue; // Retry
                                        }
                                    }
                                }
                                catch (Exception removeEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error removing duplicate User entries: {removeEx.Message}");
                                    System.Diagnostics.Debug.WriteLine($"Stack trace: {removeEx.StackTrace}");
                                }
                            }
                            
                            // If we still can't resolve it, for User entities, try one final aggressive removal
                            if (typeof(T) == typeof(User))
                            {
                                System.Diagnostics.Debug.WriteLine($"=== FINAL ATTEMPT: Removing ALL User entries from change tracker ===");
                                
                                // Get all User entries regardless of state
                                var allUserEntries = context.ChangeTracker.Entries<User>().ToList();
                                System.Diagnostics.Debug.WriteLine($"Found {allUserEntries.Count} User entries in change tracker");
                                
                                foreach (var entry in allUserEntries)
                                {
                                    System.Diagnostics.Debug.WriteLine($"  Detaching User: '{entry.Entity.Username}' (ID: {GetId(entry.Entity)}, State: {entry.State})");
                                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                }
                                
                                // Clear the change tracker completely for User entities
                                context.ChangeTracker.Entries<User>().ToList().ForEach(e => e.State = Microsoft.EntityFrameworkCore.EntityState.Detached);
                                
                                // Try one final save
                                try
                                {
                                    var finalSaveResult = await context.SaveChangesAsync();
                                    System.Diagnostics.Debug.WriteLine($"✓ Final aggressive removal succeeded: SaveChanges completed. Rows affected: {finalSaveResult}");
                                    return; // Success - we prevented the sync failure
                                }
                                catch (Exception finalSaveEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"✗ Final aggressive removal SaveChanges still failed: {finalSaveEx.Message}");
                                    // Continue to throw the original error
                                }
                            }
                            
                            // If we still can't resolve it, throw the error
                            throw new Exception($"Error saving {dbType} changes for {tableName}: Unique key violation detected. Could not find existing record to update. Conflicting value: {conflictingValue ?? "unknown"}", dbEx);
                        }
                    }
                    else
                    {
                        throw new Exception($"Error saving {dbType} changes for {tableName}: {innerMsg}. This may be due to foreign key constraints or duplicate IDs.", dbEx);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error saving {dbType} changes for {tableName}: {ex.Message}", ex);
                }
            }

            // Final catch-all: For User entities with unique key violations, remove all added entries to prevent sync failure
            if (typeof(T) == typeof(User))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"=== FINAL CATCH-ALL: Removing all added User entries to prevent sync failure ===");
                    var allAddedUsers = context.ChangeTracker.Entries<User>()
                        .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                        .ToList();
                    
                    if (allAddedUsers.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Removing {allAddedUsers.Count} added User entries as final fallback");
                        foreach (var entry in allAddedUsers)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Removing: '{entry.Entity.Username}' (ID: {GetId(entry.Entity)})");
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        
                        // Try one more time after removing all added entries
                        try
                        {
                            var saveResult = await context.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine($"✓ Final catch-all succeeded: SaveChanges completed. Rows affected: {saveResult}");
                            return; // Success - we prevented the sync failure
                        }
                        catch (Exception finalEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Final catch-all SaveChanges still failed: {finalEx.Message}");
                            // Continue to throw the original error
                        }
                    }
                }
                catch (Exception catchAllEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in final catch-all: {catchAllEx.Message}");
                }
            }
            
            throw new Exception($"Error saving {dbType} changes for {tableName}: Unique key violation could not be resolved after {maxRetries} attempts.");
        }

        private string? ExtractConflictingValueFromError(string errorMessage)
        {
            try
            {
                // SQL Server error format: "Cannot insert duplicate key in object 'dbo.Users'. The duplicate key value is (username)."
                // Look for pattern: "duplicate key value is (value)"
                var startIndex = errorMessage.IndexOf("duplicate key value is (", StringComparison.OrdinalIgnoreCase);
                if (startIndex >= 0)
                {
                    startIndex += "duplicate key value is (".Length;
                    var endIndex = errorMessage.IndexOf(")", startIndex);
                    if (endIndex > startIndex)
                    {
                        var value = errorMessage.Substring(startIndex, endIndex - startIndex).Trim();
                        // Remove quotes if present
                        value = value.Trim('\'', '"');
                        return value;
                    }
                }
            }
            catch
            {
                // If extraction fails, return null
            }
            return null;
        }

        private async Task<bool> HandleUniqueKeyViolations<T>(
            AppDbContext context, 
            DbSet<T> dbSet, 
            TableSyncSummary summary, 
            SyncResult result, 
            string tableName,
            string? conflictingValue = null) where T : class
        {
            try
            {
                // Create a fresh context for searching to avoid state issues
                AppDbContext? freshContext = null;
                try
                {
                    // Get connection string from the current context
                    var connectionString = context.Database.GetConnectionString();
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        // Fallback: use the context we have
                        freshContext = context;
                    }
                    else
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                        optionsBuilder.UseSqlServer(connectionString);
                        freshContext = new AppDbContext(optionsBuilder.Options);
                    }
                }
                catch
                {
                    // If we can't create a fresh context, use the existing one
                    freshContext = context;
                }
                
                // Capture the added entries and their entities before we start modifying
                var entriesToCheck = context.ChangeTracker.Entries<T>()
                    .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                    .Select(e => new { Entry = e, Entity = e.Entity })
                    .ToList();

                if (!entriesToCheck.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"No added entries found for {tableName} to check for unique key violations");
                    if (freshContext != context && freshContext != null)
                    {
                        await freshContext.DisposeAsync();
                    }
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Checking {entriesToCheck.Count} added {tableName} entries for unique key violations");

                bool resolvedAny = false;

                foreach (var item in entriesToCheck)
                {
                    var entry = item.Entry;
                    var addedEntity = item.Entity;
                    
                    // Log the entity details for debugging
                    if (typeof(T) == typeof(User))
                    {
                        var user = addedEntity as User;
                        System.Diagnostics.Debug.WriteLine($"Checking User with username: '{user?.Username}' (ID: {GetId(addedEntity)})");
                    }
                    
                    // First, check if this entity is already tracked in a different state
                    var trackedEntry = context.ChangeTracker.Entries<T>()
                        .FirstOrDefault(e => e.Entity != addedEntity && 
                                            ((typeof(T) == typeof(User) && 
                                              ((User)(object)e.Entity).Username == ((User)(object)addedEntity).Username) ||
                                             (typeof(T) != typeof(User) && GetId(e.Entity) == GetId(addedEntity))));
                    
                    if (trackedEntry != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found existing tracked {tableName} entity in change tracker (State: {trackedEntry.State})");
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        var existingId = GetId(trackedEntry.Entity);
                        var existing = await GetEntityForUpdateAsync(dbSet, context, existingId);
                        if (existing != null)
                        {
                            context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            await UpdateEntityValues(existing, addedEntity, context);
                            summary.Updated++;
                            result.TotalUpdated++;
                            summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                            summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                            resolvedAny = true;
                            System.Diagnostics.Debug.WriteLine($"Successfully updated existing tracked {tableName} record ID {existingId} instead of inserting");
                            continue; // Move to next entry
                        }
                    }
                    
                    // Try to find existing record by unique key
                    // First try with original context (might already have it loaded)
                    T? existingByUniqueKey = null;
                    
                    if (typeof(T) == typeof(User))
                    {
                        var user = addedEntity as User;
                        var searchUsernames = new List<string>();
                        
                        // Collect all possible username variations to search
                        if (!string.IsNullOrEmpty(user?.Username))
                        {
                            searchUsernames.Add(user.Username);
                            searchUsernames.Add(user.Username.Trim());
                        }
                        if (!string.IsNullOrEmpty(conflictingValue))
                        {
                            searchUsernames.Add(conflictingValue);
                            searchUsernames.Add(conflictingValue.Trim());
                        }
                        // Remove duplicates
                        searchUsernames = searchUsernames.Distinct().ToList();
                        
                        System.Diagnostics.Debug.WriteLine($"Searching for User with usernames: {string.Join(", ", searchUsernames.Select(u => $"'{u}'"))}");
                        
                        // Try each username variation with both contexts
                        foreach (var searchUsername in searchUsernames)
                        {
                            if (string.IsNullOrEmpty(searchUsername))
                                continue;
                                
                            // Try original context first
                            var foundUser = await FindByUniqueKeyValue(searchUsername, context);
                            if (foundUser != null)
                            {
                                existingByUniqueKey = foundUser as T;
                                System.Diagnostics.Debug.WriteLine($"Found User in original context by username '{searchUsername}': {foundUser.Username} (ID: {GetId(foundUser)})");
                                break;
                            }
                            
                            // Try fresh context
                            if (freshContext != context)
                            {
                                foundUser = await FindByUniqueKeyValue(searchUsername, freshContext);
                                if (foundUser != null)
                                {
                                    existingByUniqueKey = foundUser as T;
                                    System.Diagnostics.Debug.WriteLine($"Found User in fresh context by username '{searchUsername}': {foundUser.Username} (ID: {GetId(foundUser)})");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // For non-User entities, use the original logic
                        existingByUniqueKey = await FindByUniqueKey<T>(addedEntity, context);
                        if (existingByUniqueKey == null && freshContext != context)
                        {
                            existingByUniqueKey = await FindByUniqueKey<T>(addedEntity, freshContext);
                        }
                    }
                    
                    // Last resort: For User entities, load all users and do comprehensive search
                    if (existingByUniqueKey == null && typeof(T) == typeof(User))
                    {
                        var user = addedEntity as User;
                        System.Diagnostics.Debug.WriteLine($"Attempting comprehensive search for User. Entity username: '{user?.Username}', Conflicting value: '{conflictingValue}'");
                        
                        // Try both contexts
                        var contextsToTry = new List<AppDbContext> { context };
                        if (freshContext != context)
                        {
                            contextsToTry.Add(freshContext);
                        }
                        
                        foreach (var searchContext in contextsToTry)
                        {
                            try
                            {
                                var allUsers = await searchContext.Users.AsNoTracking().ToListAsync();
                                System.Diagnostics.Debug.WriteLine($"Loaded {allUsers.Count} users from {searchContext.GetType().Name} for comparison");
                                
                                // Declare variables outside the loop to avoid shadowing
                                var entityUsername = user?.Username?.Trim().ToLower() ?? "";
                                var conflictUsername = conflictingValue?.Trim().ToLower() ?? "";
                                
                                // Normalize usernames (remove special characters for comparison)
                                var normalizedEntityUsername = System.Text.RegularExpressions.Regex.Replace(entityUsername, @"[^a-zA-Z0-9]", "");
                                var normalizedConflictUsername = System.Text.RegularExpressions.Regex.Replace(conflictUsername, @"[^a-zA-Z0-9]", "");
                                
                                // Log all usernames for debugging (up to 50)
                                var usersToLog = allUsers.Take(50).ToList();
                                foreach (var u in usersToLog)
                                {
                                    var isMatch = false;
                                    var dbUsername = u.Username?.Trim().ToLower() ?? "";
                                    var normalizedDbUsername = System.Text.RegularExpressions.Regex.Replace(dbUsername, @"[^a-zA-Z0-9]", "");
                                    
                                    if (dbUsername == entityUsername || 
                                        (!string.IsNullOrEmpty(conflictUsername) && dbUsername == conflictUsername) ||
                                        normalizedDbUsername == normalizedEntityUsername ||
                                        (!string.IsNullOrEmpty(normalizedConflictUsername) && normalizedDbUsername == normalizedConflictUsername))
                                    {
                                        isMatch = true;
                                    }
                                    
                                    System.Diagnostics.Debug.WriteLine($"  - User ID {u.UserID}: Username='{u.Username}' (lower: '{dbUsername}', normalized: '{normalizedDbUsername}') {(isMatch ? "*** MATCH ***" : "")}");
                                }
                                
                                var matchingUser = allUsers.FirstOrDefault(u => 
                                {
                                    if (string.IsNullOrEmpty(u.Username))
                                        return false;
                                    
                                    var dbUsername = u.Username.Trim().ToLower();
                                    var normalizedDbUsername = System.Text.RegularExpressions.Regex.Replace(dbUsername, @"[^a-zA-Z0-9]", "");
                                    
                                    return dbUsername == entityUsername || 
                                           (!string.IsNullOrEmpty(conflictUsername) && dbUsername == conflictUsername) ||
                                           normalizedDbUsername == normalizedEntityUsername ||
                                           (!string.IsNullOrEmpty(normalizedConflictUsername) && normalizedDbUsername == normalizedConflictUsername);
                                });
                                
                                if (matchingUser != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Found matching user in comprehensive search from {searchContext.GetType().Name}: '{matchingUser.Username}' (ID: {matchingUser.UserID})");
                                    existingByUniqueKey = matchingUser as T;
                                    break; // Found it, stop searching
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"No matching user found in {searchContext.GetType().Name}. Searched for: entity='{entityUsername}' (normalized: '{normalizedEntityUsername}'), conflict='{conflictUsername}' (normalized: '{normalizedConflictUsername}')");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in comprehensive user search with {searchContext.GetType().Name}: {ex.Message}");
                            }
                        }
                        
                        // Final fallback: Direct SQL query to database
                        if (existingByUniqueKey == null)
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"Attempting direct SQL query as final fallback for username: '{conflictingValue ?? user?.Username}'");
                                var searchTerms = new List<string>();
                                if (!string.IsNullOrEmpty(user?.Username))
                                {
                                    searchTerms.Add(user.Username);
                                    searchTerms.Add(user.Username.Trim());
                                }
                                if (!string.IsNullOrEmpty(conflictingValue))
                                {
                                    searchTerms.Add(conflictingValue);
                                    searchTerms.Add(conflictingValue.Trim());
                                }
                                searchTerms = searchTerms.Distinct().ToList();
                                
                                foreach (var searchTerm in searchTerms)
                                {
                                    if (string.IsNullOrEmpty(searchTerm))
                                        continue;
                                        
                                    var sql = "SELECT * FROM Users WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@p0)))";
                                    var sqlUsers = await context.Users
                                        .FromSqlRaw(sql, new Microsoft.Data.SqlClient.SqlParameter("@p0", searchTerm))
                                        .AsNoTracking()
                                        .ToListAsync();
                                    
                                    if (sqlUsers.Any())
                                    {
                                        var foundUser = sqlUsers.First();
                                        System.Diagnostics.Debug.WriteLine($"Found User via direct SQL query: '{foundUser.Username}' (ID: {foundUser.UserID})");
                                        existingByUniqueKey = foundUser as T;
                                        break;
                                    }
                                }
                            }
                            catch (Exception sqlEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in direct SQL query fallback: {sqlEx.Message}");
                            }
                        }
                    }
                    
                    if (existingByUniqueKey != null)
                    {
                        // Detach the added entity
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        
                        // Get the existing record ID
                        var existingId = GetId(existingByUniqueKey);
                        
                        System.Diagnostics.Debug.WriteLine($"Found existing record with ID {existingId}, updating instead of inserting");
                        
                        // Find and update the existing record (use original context for this)
                        var existing = await GetEntityForUpdateAsync(dbSet, context, existingId);
                        if (existing != null)
                        {
                            context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            await UpdateEntityValues(existing, addedEntity, context);
                            summary.Updated++;
                            result.TotalUpdated++;
                            summary.Uploaded = Math.Max(0, summary.Uploaded - 1);
                            summary.Downloaded = Math.Max(0, summary.Downloaded - 1);
                            resolvedAny = true;
                            System.Diagnostics.Debug.WriteLine($"Successfully updated existing {tableName} record ID {existingId} instead of inserting");
                        }
                    }
                }

                // Dispose fresh context if we created one
                if (freshContext != context && freshContext != null)
                {
                    await freshContext.DisposeAsync();
                }

                return resolvedAny;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleUniqueKeyViolations for {tableName}: {ex.Message}");
                return false;
            }
        }

        private async Task<T?> FindByUniqueKey<T>(T entity, AppDbContext context) where T : class
        {
            if (typeof(T) == typeof(User))
            {
                var user = entity as User;
                if (user != null && !string.IsNullOrEmpty(user.Username))
                {
                    return await FindByUniqueKeyValue(user.Username, context) as T;
                }
            }
            else if (typeof(T) == typeof(Trainer))
            {
                var trainer = entity as Trainer;
                if (trainer != null)
                {
                    return await FindTrainerByUniqueFields(trainer, context) as T;
                }
            }
            else if (typeof(T) == typeof(Member))
            {
                var member = entity as Member;
                if (member != null)
                {
                    return await FindMemberByUniqueFields(member, context) as T;
                }
            }
            else if (typeof(T) == typeof(Payment))
            {
                var payment = entity as Payment;
                if (payment != null)
                {
                    return await FindPaymentByUniqueFields(payment, context) as T;
                }
            }
            else if (typeof(T) == typeof(Attendance))
            {
                var attendance = entity as Attendance;
                if (attendance != null)
                {
                    return await FindAttendanceByUniqueFields(attendance, context) as T;
                }
            }
            else if (typeof(T) == typeof(Promotion))
            {
                var promotion = entity as Promotion;
                if (promotion != null)
                {
                    return await FindPromotionByUniqueFields(promotion, context) as T;
                }
            }
            return null;
        }

        private async Task<Member?> FindMemberByUniqueFields(Member member, AppDbContext context)
        {
            if (member == null)
                return null;

            var email = member.Email?.Trim().ToLower() ?? "";
            var contactNumber = member.ContactNumber?.Trim() ?? "";

            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(contactNumber))
                return null;

            try
            {
                // Primary: Match by Email (most reliable - should be unique)
                if (!string.IsNullOrEmpty(email))
                {
                    var matchingMember = await context.Members
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Email.Trim().ToLower() == email);

                    if (matchingMember != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found Member by email: '{matchingMember.Email}' (ID: {matchingMember.MemberID})");
                        return matchingMember;
                    }
                }

                // Fallback: Match by ContactNumber (if provided and email didn't match)
                if (!string.IsNullOrEmpty(contactNumber))
                {
                    var byContact = await context.Members
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.ContactNumber.Trim() == contactNumber);
                    
                    if (byContact != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found Member by contact number: '{byContact.ContactNumber}' (ID: {byContact.MemberID})");
                        return byContact;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding Member by unique fields: {ex.Message}");
            }

            return null;
        }

        private async Task<Payment?> FindPaymentByUniqueFields(Payment payment, AppDbContext context)
        {
            if (payment == null)
                return null;

            try
            {
                // Match by MemberID + PaymentDate + Amount + PaymentType (combination should be unique for a payment)
                var matchingPayment = await context.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => 
                        p.MemberID == payment.MemberID &&
                        p.PaymentDate.Date == payment.PaymentDate.Date &&
                        p.Amount == payment.Amount &&
                        p.PaymentType == payment.PaymentType);

                if (matchingPayment != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found Payment by unique fields: MemberID={matchingPayment.MemberID}, Date={matchingPayment.PaymentDate:yyyy-MM-dd}, Amount={matchingPayment.Amount}, Type={matchingPayment.PaymentType} (ID: {matchingPayment.PaymentID})");
                    return matchingPayment;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding Payment by unique fields: {ex.Message}");
            }

            return null;
        }

        private async Task<Attendance?> FindAttendanceByUniqueFields(Attendance attendance, AppDbContext context)
        {
            if (attendance == null)
                return null;

            try
            {
                // Match by MemberID and CheckinTime (within 1 second tolerance to account for millisecond differences)
                var checkinTime = attendance.CheckinTime;
                var timeWindowStart = checkinTime.AddSeconds(-1);
                var timeWindowEnd = checkinTime.AddSeconds(1);
                
                var matchingAttendance = await context.Attendances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => 
                        a.MemberID == attendance.MemberID &&
                        a.CheckinTime >= timeWindowStart &&
                        a.CheckinTime <= timeWindowEnd);

                if (matchingAttendance != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found matching Attendance: MemberID={attendance.MemberID}, CheckinTime={attendance.CheckinTime} (matched with AttendanceID={matchingAttendance.AttendanceID}, CheckinTime={matchingAttendance.CheckinTime})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No matching Attendance found: MemberID={attendance.MemberID}, CheckinTime={attendance.CheckinTime}");
                }

                return matchingAttendance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding Attendance by unique fields: {ex.Message}");
                return null;
            }
        }

        private async Task<Trainer?> FindTrainerByUniqueFields(Trainer trainer, AppDbContext context)
        {
            if (trainer == null)
                return null;

            var firstName = trainer.FirstName?.Trim() ?? "";
            var lastName = trainer.LastName?.Trim() ?? "";
            var contactNumber = trainer.ContactNumber?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(contactNumber))
                return null;

            try
            {
                // Primary: Match by ContactNumber (most reliable - should be unique)
                if (!string.IsNullOrEmpty(contactNumber))
                {
                    var byContact = await context.Trainers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.ContactNumber.Trim() == contactNumber);
                    
                    if (byContact != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found Trainer by contact number: '{byContact.ContactNumber}' (ID: {byContact.TrainerID}) - '{byContact.FirstName} {byContact.LastName}'");
                        return byContact;
                    }
                }

                // Fallback: Match by FirstName + LastName (case-insensitive)
                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                {
                    var matchingTrainer = await context.Trainers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t =>
                            t.FirstName.Trim().ToLower() == firstName.ToLower() &&
                            t.LastName.Trim().ToLower() == lastName.ToLower());

                    if (matchingTrainer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found Trainer by name: '{matchingTrainer.FirstName} {matchingTrainer.LastName}' (ID: {matchingTrainer.TrainerID}) - Contact: '{matchingTrainer.ContactNumber}'");
                        return matchingTrainer;
                    }
                }

                // Last resort: Match by FirstName only (if last name is missing)
                if (!string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                {
                    var byFirstName = await context.Trainers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.FirstName.Trim().ToLower() == firstName.ToLower());
                    
                    if (byFirstName != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found Trainer by first name only: '{byFirstName.FirstName}' (ID: {byFirstName.TrainerID})");
                        return byFirstName;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding Trainer by unique fields: {ex.Message}");
            }

            return null;
        }

        private async Task<Promotion?> FindPromotionByUniqueFields(Promotion promotion, AppDbContext context)
        {
            if (promotion == null)
                return null;

            var promoName = promotion.PromoName?.Trim() ?? "";

            if (string.IsNullOrEmpty(promoName))
                return null;

            try
            {
                // Match by PromoName, StartDate, EndDate, and DiscountRate (true unique key for promotions)
                var matchingPromotion = await context.Promotions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.PromoName.Trim().ToLower() == promoName.ToLower() &&
                        p.StartDate.Date == promotion.StartDate.Date &&
                        p.EndDate.Date == promotion.EndDate.Date &&
                        p.DiscountRate == promotion.DiscountRate);

                if (matchingPromotion != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found matching Promotion: '{matchingPromotion.PromoName}' (ID: {matchingPromotion.PromoID}) - Dates: {matchingPromotion.StartDate:MMM dd, yyyy} to {matchingPromotion.EndDate:MMM dd, yyyy}, Discount: {matchingPromotion.DiscountRate}%");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No matching Promotion found: '{promoName}' - Dates: {promotion.StartDate:MMM dd, yyyy} to {promotion.EndDate:MMM dd, yyyy}, Discount: {promotion.DiscountRate}%");
                }

                return matchingPromotion;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding Promotion by unique fields: {ex.Message}");
                return null;
            }
        }

        private async Task<User?> FindByUniqueKeyValue(string username, AppDbContext context)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var searchTerm = username.Trim().ToLower();
            System.Diagnostics.Debug.WriteLine($"FindByUniqueKeyValue: Searching for username '{username}' (normalized: '{searchTerm}')");

            try
            {
                // Strategy 1: Direct SQL query with multiple variations (most reliable)
                try
                {
                    // Try exact match first
                    var sqlQuery = $"SELECT * FROM Users WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@p0)))";
                    var users = await context.Users
                        .FromSqlRaw(sqlQuery, new Microsoft.Data.SqlClient.SqlParameter("@p0", username))
                        .AsNoTracking()
                        .ToListAsync();
                    
                    if (users.Any())
                    {
                        var found = users.First();
                        System.Diagnostics.Debug.WriteLine($"Found User via SQL query (exact): '{found.Username}' (ID: {found.UserID})");
                        return found;
                    }
                    
                    // Try with trimmed search term
                    var trimmedSearchTerm = searchTerm.Trim();
                    if (trimmedSearchTerm != searchTerm)
                    {
                        users = await context.Users
                            .FromSqlRaw(sqlQuery, new Microsoft.Data.SqlClient.SqlParameter("@p0", trimmedSearchTerm))
                            .AsNoTracking()
                            .ToListAsync();
                        
                        if (users.Any())
                        {
                            var found = users.First();
                            System.Diagnostics.Debug.WriteLine($"Found User via SQL query (trimmed): '{found.Username}' (ID: {found.UserID})");
                            return found;
                        }
                    }
                }
                catch (Exception sqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL query strategy failed: {sqlEx.Message}");
                }

                // Strategy 2: Load all users and filter in memory (case-insensitive)
                var allUsers = await context.Users.AsNoTracking().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {allUsers.Count} users from database for in-memory search");

                // Exact match (case-insensitive, trimmed)
                var exactMatch = allUsers.FirstOrDefault(u => 
                    !string.IsNullOrEmpty(u.Username) && 
                    u.Username.Trim().ToLower() == searchTerm);
                
                if (exactMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found User via exact match: '{exactMatch.Username}' (ID: {exactMatch.UserID})");
                    return exactMatch;
                }

                // Strategy 3: Match ignoring dots and special characters
                var normalizedSearchTerm = System.Text.RegularExpressions.Regex.Replace(searchTerm, @"[^a-zA-Z0-9]", "");
                var normalizedMatch = allUsers.FirstOrDefault(u => 
                {
                    if (string.IsNullOrEmpty(u.Username))
                        return false;
                    var dbUsername = System.Text.RegularExpressions.Regex.Replace(u.Username.Trim().ToLower(), @"[^a-zA-Z0-9]", "");
                    return dbUsername == normalizedSearchTerm;
                });
                
                if (normalizedMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found User via normalized match: '{normalizedMatch.Username}' (ID: {normalizedMatch.UserID})");
                    return normalizedMatch;
                }

                // Strategy 4: Contains match (for partial matches)
                var containsMatch = allUsers.FirstOrDefault(u => 
                    !string.IsNullOrEmpty(u.Username) && 
                    u.Username.Trim().ToLower().Contains(searchTerm));
                
                if (containsMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found User via contains match: '{containsMatch.Username}' (ID: {containsMatch.UserID})");
                    return containsMatch;
                }

                // Strategy 5: Reverse contains (search term contains database username)
                var reverseMatch = allUsers.FirstOrDefault(u => 
                    !string.IsNullOrEmpty(u.Username) && 
                    searchTerm.Contains(u.Username.Trim().ToLower()));
                
                if (reverseMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found User via reverse contains match: '{reverseMatch.Username}' (ID: {reverseMatch.UserID})");
                    return reverseMatch;
                }

                System.Diagnostics.Debug.WriteLine($"No User found matching '{username}' after all search strategies");
                // Log all usernames for debugging (first 20)
                var sampleUsers = allUsers.Take(20).Select(u => $"'{u.Username}'").ToList();
                System.Diagnostics.Debug.WriteLine($"Sample usernames in database: {string.Join(", ", sampleUsers)}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FindByUniqueKeyValue: {ex.Message}");
                return null;
            }
        }

        private async Task FixForeignKeys<T>(T entity, AppDbContext context) where T : class
        {
            try
            {
                if (typeof(T) == typeof(Member))
                {
                    var member = entity as Member;
                    if (member != null)
                    {
                        // Fix TrainerID
                        if (member.TrainerID.HasValue && member.TrainerID.Value > 0)
                        {
                            var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == member.TrainerID.Value);
                            if (!trainerExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fixing Member: TrainerID {member.TrainerID.Value} doesn't exist, setting to null.");
                                member.TrainerID = null;
                            }
                        }
                        
                        // Fix TrainerScheduleID
                        if (member.TrainerScheduleID.HasValue && member.TrainerScheduleID.Value > 0)
                        {
                            var scheduleExists = await context.TrainerSchedules.AnyAsync(ts => ts.TrainerScheduleID == member.TrainerScheduleID.Value);
                            if (!scheduleExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fixing Member: TrainerScheduleID {member.TrainerScheduleID.Value} doesn't exist, setting to null.");
                                member.TrainerScheduleID = null;
                            }
                        }
                    }
                }
                else if (typeof(T) == typeof(User))
                {
                    var user = entity as User;
                    if (user != null && user.TrainerID.HasValue && user.TrainerID.Value > 0)
                    {
                        var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == user.TrainerID.Value);
                        if (!trainerExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fixing User: TrainerID {user.TrainerID.Value} doesn't exist, setting to null.");
                            user.TrainerID = null;
                        }
                    }
                }
                else if (typeof(T) == typeof(WalkIn))
                {
                    var walkIn = entity as WalkIn;
                    if (walkIn != null)
                    {
                        // Fix TrainerID
                        if (walkIn.TrainerID.HasValue && walkIn.TrainerID.Value > 0)
                        {
                            var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == walkIn.TrainerID.Value);
                            if (!trainerExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fixing WalkIn: TrainerID {walkIn.TrainerID.Value} doesn't exist, setting to null.");
                                walkIn.TrainerID = null;
                            }
                        }
                        
                        // Fix TrainerScheduleID
                        if (walkIn.TrainerScheduleID.HasValue && walkIn.TrainerScheduleID.Value > 0)
                        {
                            var scheduleExists = await context.TrainerSchedules.AnyAsync(ts => ts.TrainerScheduleID == walkIn.TrainerScheduleID.Value);
                            if (!scheduleExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fixing WalkIn: TrainerScheduleID {walkIn.TrainerScheduleID.Value} doesn't exist, setting to null.");
                                walkIn.TrainerScheduleID = null;
                            }
                        }
                    }
                }
                else if (typeof(T) == typeof(TrainerSchedule))
                {
                    var schedule = entity as TrainerSchedule;
                    if (schedule != null)
                    {
                        var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == schedule.TrainerID);
                        if (!trainerExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fixing TrainerSchedule: TrainerID {schedule.TrainerID} doesn't exist, cannot fix automatically (TrainerID is required).");
                            // Can't fix this - TrainerID is required for TrainerSchedule
                        }
                    }
                }
                else if (typeof(T) == typeof(Attendance))
                {
                    var attendance = entity as Attendance;
                    if (attendance != null)
                    {
                        var memberExists = await context.Members.AnyAsync(m => m.MemberID == attendance.MemberID);
                        if (!memberExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fixing Attendance: MemberID {attendance.MemberID} doesn't exist, cannot fix automatically (MemberID is required).");
                            // Can't fix this - MemberID is required for Attendance
                            // The record will be skipped during sync
                        }
                    }
                }
                else if (typeof(T) == typeof(Payment))
                {
                    var payment = entity as Payment;
                    if (payment != null)
                    {
                        var memberExists = await context.Members.AnyAsync(m => m.MemberID == payment.MemberID);
                        if (!memberExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fixing Payment: MemberID {payment.MemberID} doesn't exist, cannot fix automatically (MemberID is required).");
                            // Can't fix this - MemberID is required for Payment
                            // The record will be skipped during sync
                        }
                    }
                }
                else if (typeof(T) == typeof(Notification))
                {
                    var notification = entity as Notification;
                    if (notification != null)
                    {
                        var memberExists = await context.Members.AnyAsync(m => m.MemberID == notification.MemberID);
                        if (!memberExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fixing Notification: MemberID {notification.MemberID} doesn't exist, cannot fix automatically (MemberID is required).");
                            // Can't fix this - MemberID is required for Notification
                            // The record will be skipped during sync
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fixing foreign keys for {typeof(T).Name}: {ex.Message}");
            }
        }

        private async Task<bool> ValidateForeignKeys<T>(T entity, AppDbContext context) where T : class
        {
            try
            {
                if (typeof(T) == typeof(Member))
                {
                    var member = entity as Member;
                    if (member != null)
                    {
                        // Validate MembershipTypeID
                        if (member.MembershipTypeID > 0)
                    {
                        var exists = await context.MembershipTypes.AnyAsync(mt => mt.MembershipTypeID == member.MembershipTypeID);
                        if (!exists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Member references non-existent MembershipTypeID: {member.MembershipTypeID}");
                            return false;
                            }
                        }
                        // Validate TrainerID if present
                        if (member.TrainerID.HasValue && member.TrainerID.Value > 0)
                        {
                            var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == member.TrainerID.Value);
                            if (!trainerExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"Member references non-existent TrainerID: {member.TrainerID.Value}. Setting TrainerID to null.");
                                // Set TrainerID to null to allow sync to proceed
                                member.TrainerID = null;
                            }
                        }
                    }
                }
                else if (typeof(T) == typeof(User))
                {
                    var user = entity as User;
                    if (user != null)
                    {
                        if (user.RoleID > 0)
                        {
                            var roleExists = await context.Roles.AnyAsync(r => r.RoleID == user.RoleID);
                            if (!roleExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"User references non-existent RoleID: {user.RoleID}");
                                return false;
                            }
                        }
                        // TrainerID validation is now handled by FixForeignKeys - it will set to null if invalid
                        // So we don't fail validation here, just let FixForeignKeys handle it
                    }
                }
                else if (typeof(T) == typeof(WalkIn))
                {
                    var walkIn = entity as WalkIn;
                    if (walkIn != null)
                    {
                        // Validate TrainerID if present (FixForeignKeys will set to null if invalid)
                        if (walkIn.TrainerID.HasValue && walkIn.TrainerID.Value > 0)
                        {
                            var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == walkIn.TrainerID.Value);
                            if (!trainerExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"WalkIn references non-existent TrainerID: {walkIn.TrainerID.Value}. Setting TrainerID to null.");
                                walkIn.TrainerID = null;
                            }
                        }
                        
                        // Validate TrainerScheduleID if present
                        if (walkIn.TrainerScheduleID.HasValue && walkIn.TrainerScheduleID.Value > 0)
                        {
                            var scheduleExists = await context.TrainerSchedules.AnyAsync(ts => ts.TrainerScheduleID == walkIn.TrainerScheduleID.Value);
                            if (!scheduleExists)
                            {
                                System.Diagnostics.Debug.WriteLine($"WalkIn references non-existent TrainerScheduleID: {walkIn.TrainerScheduleID.Value}. Setting TrainerScheduleID to null.");
                                walkIn.TrainerScheduleID = null;
                            }
                        }
                    }
                }
                else if (typeof(T) == typeof(TrainerSchedule))
                {
                    var schedule = entity as TrainerSchedule;
                    if (schedule != null)
                    {
                        var trainerExists = await context.Trainers.AnyAsync(t => t.TrainerID == schedule.TrainerID);
                        if (!trainerExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"TrainerSchedule references non-existent TrainerID: {schedule.TrainerID}");
                            return false;
                        }
                    }
                }
                else if (typeof(T) == typeof(Attendance))
                {
                    var attendance = entity as Attendance;
                    if (attendance != null)
                    {
                        var memberExists = await context.Members.AnyAsync(m => m.MemberID == attendance.MemberID);
                        if (!memberExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Attendance record validation failed: MemberID {attendance.MemberID} does not exist in {context.GetType().Name}");
                            return false;
                        }
                    }
                }
                else if (typeof(T) == typeof(Payment))
                {
                    var payment = entity as Payment;
                    if (payment != null)
                    {
                        var memberExists = await context.Members.AnyAsync(m => m.MemberID == payment.MemberID);
                        if (!memberExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Payment references non-existent MemberID: {payment.MemberID}");
                            return false;
                        }
                    }
                }
                else if (typeof(T) == typeof(Notification))
                {
                    var notification = entity as Notification;
                    if (notification != null)
                    {
                        var memberExists = await context.Members.AnyAsync(m => m.MemberID == notification.MemberID);
                        if (!memberExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Notification references non-existent MemberID: {notification.MemberID}");
                            return false;
                        }
                    }
                }
                else if (typeof(T) == typeof(RolePermission))
                {
                    var rolePermission = entity as RolePermission;
                    if (rolePermission != null)
                    {
                        var roleExists = await context.Roles.AnyAsync(r => r.RoleID == rolePermission.RoleID);
                        if (!roleExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"RolePermission references non-existent RoleID: {rolePermission.RoleID}");
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating foreign keys for {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }

        private T CloneEntityForAdd<T>(T source) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(source);
                var newEntity = JsonSerializer.Deserialize<T>(json);
                if (newEntity == null)
                {
                    throw new Exception("Failed to clone entity");
                }

                // Reset the ID to 0 so EF Core treats it as a new entity
                // This allows the database to auto-generate the ID
                var idProperty = typeof(T).GetProperties()
                    .FirstOrDefault(p => p.Name == "ID" || p.Name == "Id" || p.Name == "id" || 
                                         p.Name == "UserID" || p.Name == "MemberID" || 
                                         p.Name == "TrainerID" || p.Name == "RoleID" ||
                                         p.Name == "MembershipTypeID" || p.Name == "PromotionID" ||
                                         p.Name == "AttendanceID" || p.Name == "PaymentID" ||
                                         p.Name == "ScheduleID" || p.Name == "AuditLogID" ||
                                         p.Name == "WalkInID" || p.Name == "PromoID" ||
                                         p.Name == "NotificationID");
                
                if (idProperty != null && idProperty.CanWrite)
                {
                    // Set ID to 0 (or default value) so EF Core treats it as a new entity
                    // This allows the database to auto-generate the ID
                    var propertyType = idProperty.PropertyType;
                    object defaultValue;
                    
                    if (propertyType == typeof(int) || propertyType == typeof(int?))
                    {
                        defaultValue = 0;
                    }
                    else if (propertyType.IsValueType)
                    {
                        defaultValue = Activator.CreateInstance(propertyType);
                    }
                    else
                    {
                        defaultValue = null;
                    }
                    
                    idProperty.SetValue(newEntity, defaultValue);
                    System.Diagnostics.Debug.WriteLine($"Reset {typeof(T).Name}.{idProperty.Name} to {defaultValue} for new entity");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not find ID property for {typeof(T).Name} to reset");
                }

                // Clear navigation properties to avoid tracking issues
                var entityType = typeof(T);
                var properties = entityType.GetProperties()
                    .Where(p => IsNavigationProperty(p, entityType));

                foreach (var prop in properties)
                {
                    if (prop.CanWrite)
                    {
                        prop.SetValue(newEntity, null);
                    }
                }

                return newEntity;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cloning entity: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Inserts a Member directly in the database using raw SQL, avoiding change tracker issues with MemberPromo.
        /// </summary>
        private async Task<bool> InsertMemberDirectly(Member member, AppDbContext context)
        {
            try
            {
                // Use raw SQL to insert Member directly, avoiding change tracker
                var sql = @"INSERT INTO Members (FirstName, MiddleInitial, LastName, ContactNumber, Email, Address, JoinDate, ExpirationDate, Status, IsArchived, MembershipTypeID, TrainerID, TrainerScheduleID)
                            VALUES (@FirstName, @MiddleInitial, @LastName, @ContactNumber, @Email, @Address, @JoinDate, @ExpirationDate, @Status, @IsArchived, @MembershipTypeID, @TrainerID, @TrainerScheduleID)";
                
                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@FirstName", member.FirstName ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@MiddleInitial", member.MiddleInitial ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@LastName", member.LastName ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@ContactNumber", member.ContactNumber ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@Email", member.Email ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@Address", member.Address ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@JoinDate", member.JoinDate),
                    new Microsoft.Data.SqlClient.SqlParameter("@ExpirationDate", member.ExpirationDate ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@Status", member.Status ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@IsArchived", member.IsArchived),
                    new Microsoft.Data.SqlClient.SqlParameter("@MembershipTypeID", member.MembershipTypeID),
                    new Microsoft.Data.SqlClient.SqlParameter("@TrainerID", member.TrainerID ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@TrainerScheduleID", member.TrainerScheduleID ?? (object)DBNull.Value)
                };
                
                var rowsAffected = await context.Database.ExecuteSqlRawAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inserting Member directly: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates a Member directly in the database using ExecuteUpdate, avoiding change tracker issues with MemberPromo.
        /// </summary>
        private async Task<bool> UpdateMemberDirectly(Member source, AppDbContext context, int memberId)
        {
            try
            {
                // Use ExecuteUpdate to update directly in database without loading into change tracker
                var rowsAffected = await context.Members
                    .Where(m => m.MemberID == memberId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.FirstName, source.FirstName)
                        .SetProperty(m => m.MiddleInitial, source.MiddleInitial)
                        .SetProperty(m => m.LastName, source.LastName)
                        .SetProperty(m => m.ContactNumber, source.ContactNumber)
                        .SetProperty(m => m.Email, source.Email)
                        .SetProperty(m => m.Address, source.Address)
                        .SetProperty(m => m.JoinDate, source.JoinDate)
                        .SetProperty(m => m.ExpirationDate, source.ExpirationDate)
                        .SetProperty(m => m.Status, source.Status)
                        .SetProperty(m => m.IsArchived, source.IsArchived)
                        .SetProperty(m => m.MembershipTypeID, source.MembershipTypeID)
                        .SetProperty(m => m.TrainerID, source.TrainerID)
                        .SetProperty(m => m.TrainerScheduleID, source.TrainerScheduleID));
                
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating Member directly: {ex.Message}");
                return false;
            }
        }

        private async Task UpdateEntityValues<T>(T target, T source, AppDbContext context) where T : class
        {
            // Update scalar properties only (navigation properties are excluded via IsNavigationProperty check)
            var entityType = typeof(T);
            var properties = entityType.GetProperties()
                .Where(p => p.CanRead && p.CanWrite && 
                           !p.Name.Contains("ID") && // Exclude ID properties
                           p.Name != "UserID" && p.Name != "MemberID" && 
                           p.Name != "TrainerID" && p.Name != "RoleID" &&
                           p.Name != "MembershipTypeID" && p.Name != "PromotionID" &&
                           p.Name != "AttendanceID" && p.Name != "PaymentID" &&
                           p.Name != "ScheduleID" && p.Name != "AuditLogID" &&
                           p.Name != "NotificationID" &&
                           !IsNavigationProperty(p, entityType));

            foreach (var prop in properties)
            {
                var sourceValue = prop.GetValue(source);
                prop.SetValue(target, sourceValue);
            }
            
            // Fix foreign key references after updating (e.g., invalid TrainerID)
            await FixForeignKeys(target, context);
            
            // For Members, aggressively detach composite key entities after updating
            if (typeof(T) == typeof(Member))
            {
                DetachAllCompositeKeyEntities(context);
            }
            
            // Don't touch navigation properties at all - EF Core will handle them automatically
            // The error occurs when we try to manipulate IsLoaded, so we simply don't touch navigation properties
        }

        private bool IsNavigationProperty(System.Reflection.PropertyInfo prop, Type entityType)
        {
            // Check if property is a navigation property (collection or entity type)
            var propType = prop.PropertyType;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
            {
                return true;
            }
            if (propType.Namespace == "project.Models" && propType != entityType)
            {
                return true;
            }
            return false;
        }

        private DateTime? GetLastUpdated<T>(T entity) where T : class
        {
            var entityType = typeof(T);
            
            // Special handling for Payment - use PaymentDate as the timestamp
            if (typeof(T) == typeof(Payment))
            {
                var payment = entity as Payment;
                if (payment != null)
                {
                    return payment.PaymentDate;
                }
            }
            
            // Special handling for Member - use ExpirationDate as the timestamp
            // (ExpirationDate changes when members renew, indicating a more recent update)
            if (typeof(T) == typeof(Member))
            {
                var member = entity as Member;
                if (member != null)
                {
                    // Use ExpirationDate if available, otherwise fall back to JoinDate
                    return member.ExpirationDate ?? member.JoinDate;
                }
            }
            
            // Special handling for User - use UpdatedAt, LastPasswordChange, LastLogin, or CreatedAt
            if (typeof(T) == typeof(User))
            {
                var user = entity as User;
                if (user != null)
                {
                    // Use UpdatedAt if available, otherwise LastPasswordChange, otherwise LastLogin, otherwise CreatedAt
                    return user.UpdatedAt ?? user.LastPasswordChange ?? user.LastLogin ?? user.CreatedAt;
                }
            }
            
            // Special handling for WalkIn - use VisitDate as the timestamp
            if (typeof(T) == typeof(WalkIn))
            {
                var walkIn = entity as WalkIn;
                if (walkIn != null)
                {
                    return walkIn.VisitDate;
                }
            }
            
            // Special handling for Attendance - use CheckinTime as the timestamp
            if (typeof(T) == typeof(Attendance))
            {
                var attendance = entity as Attendance;
                if (attendance != null)
                {
                    return attendance.CheckinTime;
                }
            }
            
            var updatedAtProperty = entityType.GetProperties()
                .FirstOrDefault(p => p.Name == "UpdatedAt" && p.PropertyType == typeof(DateTime?));

            if (updatedAtProperty == null)
            {
                var createdAtProperty = entityType.GetProperties()
                    .FirstOrDefault(p => p.Name == "CreatedAt" && p.PropertyType == typeof(DateTime?));
                if (createdAtProperty != null)
                {
                    return (DateTime?)createdAtProperty.GetValue(entity);
                }
                return null;
            }

            var updatedAt = (DateTime?)updatedAtProperty.GetValue(entity);
            if (updatedAt.HasValue)
            {
                return updatedAt;
            }

            // Use a different variable name to avoid shadowing
            var createdAtProp = entityType.GetProperties()
                .FirstOrDefault(p => p.Name == "CreatedAt" && p.PropertyType == typeof(DateTime?));
            if (createdAtProp != null)
            {
                return (DateTime?)createdAtProp.GetValue(entity);
            }

            return null;
        }

        private int CountNonNullFields(User user)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(user.Username)) count++;
            if (!string.IsNullOrEmpty(user.Password)) count++;
            if (!string.IsNullOrEmpty(user.FirstName)) count++;
            if (!string.IsNullOrEmpty(user.LastName)) count++;
            if (!string.IsNullOrEmpty(user.Email)) count++;
            if (user.RoleID > 0) count++;
            if (user.IsActive.HasValue) count++;
            if (user.LastLogin.HasValue) count++;
            if (user.LastPasswordChange.HasValue) count++;
            if (user.CreatedAt.HasValue) count++;
            if (user.UpdatedAt.HasValue) count++;
            if (user.CreatedBy.HasValue) count++;
            if (user.TrainerID.HasValue) count++;
            return count;
        }

        private int CountNonNullFields(Member member)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(member.FirstName)) count++;
            if (!string.IsNullOrEmpty(member.MiddleInitial)) count++;
            if (!string.IsNullOrEmpty(member.LastName)) count++;
            if (!string.IsNullOrEmpty(member.ContactNumber)) count++;
            if (!string.IsNullOrEmpty(member.Email)) count++;
            if (!string.IsNullOrEmpty(member.Address)) count++;
            if (member.JoinDate != default(DateTime)) count++;
            if (member.ExpirationDate.HasValue) count++;
            if (!string.IsNullOrEmpty(member.Status)) count++;
            if (member.MembershipTypeID > 0) count++;
            if (member.TrainerID.HasValue) count++;
            if (member.TrainerScheduleID.HasValue) count++;
            return count;
        }

        private int CountNonNullFields(WalkIn walkIn)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(walkIn.FirstName)) count++;
            if (!string.IsNullOrEmpty(walkIn.MiddleName)) count++;
            if (!string.IsNullOrEmpty(walkIn.LastName)) count++;
            if (walkIn.VisitDate != default(DateTime)) count++;
            if (walkIn.PaymentAmount > 0) count++;
            if (!string.IsNullOrEmpty(walkIn.PaymentMethod)) count++;
            if (walkIn.TrainerID.HasValue) count++;
            if (walkIn.TrainerScheduleID.HasValue) count++;
            if (!string.IsNullOrEmpty(walkIn.PayMongoPaymentId)) count++;
            if (!string.IsNullOrEmpty(walkIn.PayMongoStatus)) count++;
            if (walkIn.IsOnlinePayment.HasValue) count++;
            return count;
        }

        private async Task<T?> FindEntityByIdAsync<T>(DbSet<T> dbSet, int id) where T : class
        {
            // Use AsNoTracking to avoid loading navigation properties
            // Build dynamic query based on entity type
            if (typeof(T) == typeof(User))
            {
                return await dbSet.AsNoTracking().Cast<User>().FirstOrDefaultAsync(u => u.UserID == id) as T;
            }
            else if (typeof(T) == typeof(Member))
            {
                return await dbSet.AsNoTracking().Cast<Member>().FirstOrDefaultAsync(m => m.MemberID == id) as T;
            }
            else if (typeof(T) == typeof(Trainer))
            {
                return await dbSet.AsNoTracking().Cast<Trainer>().FirstOrDefaultAsync(t => t.TrainerID == id) as T;
            }
            else if (typeof(T) == typeof(Role))
            {
                return await dbSet.AsNoTracking().Cast<Role>().FirstOrDefaultAsync(r => r.RoleID == id) as T;
            }
            else if (typeof(T) == typeof(MembershipType))
            {
                return await dbSet.AsNoTracking().Cast<MembershipType>().FirstOrDefaultAsync(mt => mt.MembershipTypeID == id) as T;
            }
            else if (typeof(T) == typeof(Promotion))
            {
                return await dbSet.AsNoTracking().Cast<Promotion>().FirstOrDefaultAsync(p => p.PromoID == id) as T;
            }
            else if (typeof(T) == typeof(Attendance))
            {
                return await dbSet.AsNoTracking().Cast<Attendance>().FirstOrDefaultAsync(a => a.AttendanceID == id) as T;
            }
            else if (typeof(T) == typeof(Payment))
            {
                return await dbSet.AsNoTracking().Cast<Payment>().FirstOrDefaultAsync(p => p.PaymentID == id) as T;
            }
            else if (typeof(T) == typeof(TrainerSchedule))
            {
                return await dbSet.AsNoTracking().Cast<TrainerSchedule>().FirstOrDefaultAsync(ts => ts.TrainerScheduleID == id) as T;
            }
            else if (typeof(T) == typeof(WalkIn))
            {
                return await dbSet.AsNoTracking().Cast<WalkIn>().FirstOrDefaultAsync(w => w.WalkInID == id) as T;
            }
            else if (typeof(T) == typeof(AuditLog))
            {
                return await dbSet.AsNoTracking().Cast<AuditLog>().FirstOrDefaultAsync(al => al.AuditLogID == id) as T;
            }
            else if (typeof(T) == typeof(MemberPromo))
            {
                return await dbSet.AsNoTracking().Cast<MemberPromo>().FirstOrDefaultAsync(mp => mp.Id == id) as T;
            }
            
            // If type not handled, return null (should not happen for synced entities)
            System.Diagnostics.Debug.WriteLine($"Warning: FindEntityByIdAsync called for unhandled type {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// Detaches ALL composite key entities from the context to prevent modification errors.
        /// This is called aggressively to ensure no MemberPromo or MemberTrainer entities are tracked.
        /// </summary>
        private void DetachAllCompositeKeyEntities(AppDbContext context)
        {
            try
            {
                // Get all entries in the change tracker
                var allEntries = context.ChangeTracker.Entries().ToList();
                
                // Detach ALL MemberPromo entities from the change tracker
                foreach (var entry in allEntries)
                {
                    try
                    {
                        var entityTypeName = entry.Entity.GetType().Name;
                        
                        if (entityTypeName == "MemberPromo")
                        {
                            if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                            {
                                System.Diagnostics.Debug.WriteLine($"Detaching MemberPromo entity (State: {entry.State})");
                                entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            }
                        }
                        else if (entityTypeName == "MemberTrainer")
                        {
                            if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                            {
                                System.Diagnostics.Debug.WriteLine($"Detaching MemberTrainer entity (State: {entry.State})");
                                entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error detaching entity: {ex.Message}");
                    }
                }
                
                // Double-check: Query the change tracker again to ensure they're all detached
                var remainingMemberPromos = context.ChangeTracker.Entries()
                    .Where(e => e.Entity.GetType().Name == "MemberPromo" && 
                               e.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                    .ToList();
                
                if (remainingMemberPromos.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: {remainingMemberPromos.Count} MemberPromo entities still tracked after detach attempt");
                    foreach (var entry in remainingMemberPromos)
                    {
                        try
                        {
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detaching composite key entities: {ex.Message}");
            }
        }

        /// <summary>
        /// Detaches related entities that have composite keys to prevent modification errors.
        /// </summary>
        private void DetachCompositeKeyEntities<T>(T entity, AppDbContext context) where T : class
        {
            // For Members, detach ALL composite key entities (not just related ones)
            if (typeof(T) == typeof(Member))
            {
                DetachAllCompositeKeyEntities(context);
            }
        }

        /// <summary>
        /// Safely gets an entity for updating, checking if it's already tracked first to avoid double-tracking errors.
        /// </summary>
        private async Task<T?> GetEntityForUpdateAsync<T>(DbSet<T> dbSet, AppDbContext context, int id) where T : class
        {
            // First, check if the entity is already tracked in the context
            var trackedEntity = context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e =>
                {
                    var entityId = GetId(e.Entity);
                    return entityId == id;
                })?.Entity;

            if (trackedEntity != null)
            {
                // Entity is already tracked, detach related composite key entities before returning
                DetachCompositeKeyEntities(trackedEntity, context);
                return trackedEntity;
            }

            // Entity is not tracked, load it with tracking (but without navigation properties)
            T? loadedEntity = null;
            if (typeof(T) == typeof(User))
            {
                loadedEntity = await dbSet.Cast<User>().FirstOrDefaultAsync(u => u.UserID == id) as T;
            }
            else if (typeof(T) == typeof(Member))
            {
                // First, aggressively detach ALL composite key entities from context
                DetachAllCompositeKeyEntities(context);
                
                // Load Member without navigation properties using AsNoTracking
                var memberNoTracking = await dbSet.Cast<Member>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MemberID == id);
                
                if (memberNoTracking != null)
                {
                    // Create a completely new Member instance with only scalar properties
                    // This ensures no navigation properties are included
                    var cleanMember = new Member
                    {
                        MemberID = memberNoTracking.MemberID,
                        FirstName = memberNoTracking.FirstName,
                        MiddleInitial = memberNoTracking.MiddleInitial,
                        LastName = memberNoTracking.LastName,
                        ContactNumber = memberNoTracking.ContactNumber,
                        Email = memberNoTracking.Email,
                        Address = memberNoTracking.Address,
                        JoinDate = memberNoTracking.JoinDate,
                        ExpirationDate = memberNoTracking.ExpirationDate,
                        Status = memberNoTracking.Status,
                        IsArchived = memberNoTracking.IsArchived,
                        MembershipTypeID = memberNoTracking.MembershipTypeID,
                        TrainerID = memberNoTracking.TrainerID,
                        TrainerScheduleID = memberNoTracking.TrainerScheduleID
                        // Explicitly NOT setting any navigation properties
                    };
                    
                    // Detach all composite key entities again
                    DetachAllCompositeKeyEntities(context);
                    
                    // Attach the clean Member instance for tracking
                    context.Entry(cleanMember).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    loadedEntity = cleanMember as T;
                    
                    // Final detach before returning
                    DetachAllCompositeKeyEntities(context);
                }
            }
            else if (typeof(T) == typeof(Trainer))
            {
                loadedEntity = await dbSet.Cast<Trainer>().FirstOrDefaultAsync(t => t.TrainerID == id) as T;
            }
            else if (typeof(T) == typeof(Role))
            {
                loadedEntity = await dbSet.Cast<Role>().FirstOrDefaultAsync(r => r.RoleID == id) as T;
            }
            else if (typeof(T) == typeof(RolePermission))
            {
                loadedEntity = await dbSet.Cast<RolePermission>().FirstOrDefaultAsync(rp => rp.Id == id) as T;
            }
            else if (typeof(T) == typeof(MembershipType))
            {
                loadedEntity = await dbSet.Cast<MembershipType>().FirstOrDefaultAsync(mt => mt.MembershipTypeID == id) as T;
            }
            else if (typeof(T) == typeof(Promotion))
            {
                loadedEntity = await dbSet.Cast<Promotion>().FirstOrDefaultAsync(p => p.PromoID == id) as T;
            }
            else if (typeof(T) == typeof(Attendance))
            {
                loadedEntity = await dbSet.Cast<Attendance>().FirstOrDefaultAsync(a => a.AttendanceID == id) as T;
            }
            else if (typeof(T) == typeof(Payment))
            {
                loadedEntity = await dbSet.Cast<Payment>().FirstOrDefaultAsync(p => p.PaymentID == id) as T;
            }
            else if (typeof(T) == typeof(TrainerSchedule))
            {
                loadedEntity = await dbSet.Cast<TrainerSchedule>().FirstOrDefaultAsync(ts => ts.TrainerScheduleID == id) as T;
            }
            else if (typeof(T) == typeof(WalkIn))
            {
                loadedEntity = await dbSet.Cast<WalkIn>().FirstOrDefaultAsync(w => w.WalkInID == id) as T;
            }
            else if (typeof(T) == typeof(AuditLog))
            {
                loadedEntity = await dbSet.Cast<AuditLog>().FirstOrDefaultAsync(al => al.AuditLogID == id) as T;
            }
            else if (typeof(T) == typeof(MemberPromo))
            {
                // Load MemberPromo with AsNoTracking to avoid composite key issues
                var memberPromoNoTracking = await dbSet.Cast<MemberPromo>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(mp => mp.Id == id);
                
                if (memberPromoNoTracking != null)
                {
                    // Attach for tracking
                    context.Entry(memberPromoNoTracking).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    loadedEntity = memberPromoNoTracking as T;
                }
            }

            if (loadedEntity != null)
            {
                // Detach any related composite key entities before returning
                DetachCompositeKeyEntities(loadedEntity, context);
            }

            return loadedEntity;
        }

        private int GetId<T>(T entity) where T : class
        {
            var entityType = typeof(T);
            
            // Special handling for MemberPromo and RolePermission - use Id property
            if (typeof(T) == typeof(MemberPromo))
            {
                var memberPromo = entity as MemberPromo;
                return memberPromo?.Id ?? 0;
            }
            if (typeof(T) == typeof(RolePermission))
            {
                var rolePermission = entity as RolePermission;
                return rolePermission?.Id ?? 0;
            }
            
            var idProperty = entityType.GetProperties()
                .FirstOrDefault(p => p.Name == "ID" || p.Name == "Id" || p.Name == "id" || 
                                    p.Name == "UserID" || p.Name == "MemberID" || 
                                    p.Name == "TrainerID" || p.Name == "RoleID" ||
                                    p.Name == "MembershipTypeID" || p.Name == "PromotionID" || p.Name == "PromoID" ||
                                    p.Name == "AttendanceID" || p.Name == "PaymentID" ||
                                    p.Name == "ScheduleID" || p.Name == "AuditLogID" ||
                                    p.Name == "WalkInID" || p.Name == "NotificationID");

            if (idProperty == null)
            {
                return 0;
            }

            var value = idProperty.GetValue(entity);
            if (value == null)
            {
                return 0;
            }

            return Convert.ToInt32(value);
        }

        public async Task<Dictionary<string, int>> GetLocalCountsAsync()
        {
            using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(LOCAL_CONNECTION_STRING).Options);
            return await GetCounts(context);
        }

        public async Task<Dictionary<string, int>> GetOnlineCountsAsync()
        {
            using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ONLINE_CONNECTION_STRING).Options);
            return await GetCounts(context);
        }

        private async Task<Dictionary<string, int>> GetCounts(AppDbContext context)
        {
            var counts = new Dictionary<string, int>();
            try
            {
                counts["Roles"] = await context.Roles.CountAsync();
                counts["Users"] = await context.Users.CountAsync();
                counts["MembershipTypes"] = await context.MembershipTypes.CountAsync();
                counts["Promotions"] = await context.Promotions.CountAsync();
                counts["Trainers"] = await context.Trainers.CountAsync();
                counts["TrainerSchedules"] = await context.TrainerSchedules.CountAsync();
                counts["Members"] = await context.Members.CountAsync();
                counts["WalkIns"] = await context.WalkIns.CountAsync();
                counts["Attendances"] = await context.Attendances.CountAsync();
                counts["Payments"] = await context.Payments.CountAsync();
                counts["AuditLogs"] = await context.AuditLogs.CountAsync();
                // Add missing tables to match SQL query
                counts["MemberPromos"] = await context.MemberPromos.CountAsync();
                counts["MemberTrainers"] = await context.MemberTrainers.CountAsync();
                counts["Notifications"] = await context.Notifications.CountAsync();
                counts["RolePermissions"] = await context.RolePermissions.CountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting counts: {ex.Message}");
            }
            return counts;
        }

        public async Task<DateTime?> GetLastSyncTimeAsync()
        {
            try
            {
                var lastSyncTimeString = await SecureStorage.Default.GetAsync("last_sync_time");
                if (string.IsNullOrEmpty(lastSyncTimeString))
                {
                    return null;
                }
                return DateTime.Parse(lastSyncTimeString);
            }
            catch
            {
                return null;
            }
        }

        private async Task CleanupDuplicateTrainers(AppDbContext context, SyncResult result)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting duplicate trainer cleanup...");
                
                // Load all trainers
                var allTrainers = await context.Trainers.AsNoTracking().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {allTrainers.Count} trainers for duplicate check");

                // Group trainers by FirstName + LastName + ContactNumber
                var trainerGroups = allTrainers
                    .GroupBy(t => new
                    {
                        FirstName = (t.FirstName ?? "").Trim().ToLower(),
                        LastName = (t.LastName ?? "").Trim().ToLower(),
                        ContactNumber = (t.ContactNumber ?? "").Trim()
                    })
                    .Where(g => g.Count() > 1) // Only groups with duplicates
                    .ToList();

                if (!trainerGroups.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No duplicate trainers found");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Found {trainerGroups.Count} groups of duplicate trainers");

                int duplicatesRemoved = 0;

                foreach (var group in trainerGroups)
                {
                    var duplicates = group.OrderBy(t => t.TrainerID).ToList(); // Order by ID to keep the first one
                    var keepTrainer = duplicates.First(); // Keep the trainer with the lowest ID
                    var removeTrainers = duplicates.Skip(1).ToList(); // Remove the rest

                    System.Diagnostics.Debug.WriteLine($"Found {duplicates.Count} duplicates for '{keepTrainer.FirstName} {keepTrainer.LastName}' ({keepTrainer.ContactNumber}). Keeping ID {keepTrainer.TrainerID}, removing IDs: {string.Join(", ", removeTrainers.Select(t => t.TrainerID))}");

                    // Update foreign key references before deleting
                    foreach (var duplicate in removeTrainers)
                    {
                        try
                        {
                            // Update Members that reference this duplicate trainer
                            var membersWithDuplicate = await context.Members
                                .Where(m => m.TrainerID == duplicate.TrainerID)
                                .ToListAsync();
                            
                            foreach (var member in membersWithDuplicate)
                            {
                                member.TrainerID = keepTrainer.TrainerID;
                                System.Diagnostics.Debug.WriteLine($"Updated Member ID {member.MemberID} to use Trainer ID {keepTrainer.TrainerID} instead of {duplicate.TrainerID}");
                            }

                            // Update WalkIns that reference this duplicate trainer
                            var walkInsWithDuplicate = await context.WalkIns
                                .Where(w => w.TrainerID == duplicate.TrainerID)
                                .ToListAsync();
                            
                            foreach (var walkIn in walkInsWithDuplicate)
                            {
                                walkIn.TrainerID = keepTrainer.TrainerID;
                                System.Diagnostics.Debug.WriteLine($"Updated WalkIn ID {walkIn.WalkInID} to use Trainer ID {keepTrainer.TrainerID} instead of {duplicate.TrainerID}");
                            }

                            // Update TrainerSchedules that reference this duplicate trainer
                            var schedulesWithDuplicate = await context.TrainerSchedules
                                .Where(ts => ts.TrainerID == duplicate.TrainerID)
                                .ToListAsync();
                            
                            foreach (var schedule in schedulesWithDuplicate)
                            {
                                schedule.TrainerID = keepTrainer.TrainerID;
                                System.Diagnostics.Debug.WriteLine($"Updated TrainerSchedule ID {schedule.TrainerScheduleID} to use Trainer ID {keepTrainer.TrainerID} instead of {duplicate.TrainerID}");
                            }

                            // Update MemberTrainers that reference this duplicate trainer
                            var memberTrainersWithDuplicate = await context.MemberTrainers
                                .Where(mt => mt.TrainerID == duplicate.TrainerID)
                                .ToListAsync();
                            
                            foreach (var memberTrainer in memberTrainersWithDuplicate)
                            {
                                // Check if a MemberTrainer with the same MemberID and keepTrainer.TrainerID already exists
                                var existingMemberTrainer = await context.MemberTrainers
                                    .FirstOrDefaultAsync(mt => mt.MemberID == memberTrainer.MemberID && mt.TrainerID == keepTrainer.TrainerID);
                                
                                if (existingMemberTrainer == null)
                                {
                                    // Update to point to the kept trainer
                                    memberTrainer.TrainerID = keepTrainer.TrainerID;
                                    System.Diagnostics.Debug.WriteLine($"Updated MemberTrainer (MemberID: {memberTrainer.MemberID}) to use Trainer ID {keepTrainer.TrainerID} instead of {duplicate.TrainerID}");
                                }
                                else
                                {
                                    // Remove duplicate MemberTrainer relationship
                                    context.MemberTrainers.Remove(memberTrainer);
                                    System.Diagnostics.Debug.WriteLine($"Removed duplicate MemberTrainer (MemberID: {memberTrainer.MemberID}, TrainerID: {duplicate.TrainerID})");
                                }
                            }

                            // Now delete the duplicate trainer
                            var trainerToDelete = await context.Trainers.FindAsync(duplicate.TrainerID);
                            if (trainerToDelete != null)
                            {
                                context.Trainers.Remove(trainerToDelete);
                                duplicatesRemoved++;
                                System.Diagnostics.Debug.WriteLine($"Marked Trainer ID {duplicate.TrainerID} for deletion");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error cleaning up duplicate Trainer ID {duplicate.TrainerID}: {ex.Message}");
                        }
                    }
                }

                // Save all changes
                if (duplicatesRemoved > 0)
                {
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully removed {duplicatesRemoved} duplicate trainers");
                    result.Message += $" Removed {duplicatesRemoved} duplicate trainers.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CleanupDuplicateTrainers: {ex.Message}");
                // Don't throw - allow sync to continue even if cleanup fails
            }
        }

        private async Task CleanupDuplicateMembers(AppDbContext context, SyncResult result)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting duplicate member cleanup...");
                
                // Load all members
                var allMembers = await context.Members.AsNoTracking().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {allMembers.Count} members for duplicate check");

                // Group members by Email (primary) or ContactNumber (fallback)
                var memberGroups = allMembers
                    .GroupBy(m => new
                    {
                        Email = (m.Email ?? "").Trim().ToLower(),
                        ContactNumber = (m.ContactNumber ?? "").Trim()
                    })
                    .Where(g => g.Count() > 1) // Only groups with duplicates
                    .ToList();

                if (!memberGroups.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No duplicate members found");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Found {memberGroups.Count} groups of duplicate members");

                int duplicatesRemoved = 0;

                foreach (var group in memberGroups)
                {
                    var duplicates = group.OrderBy(m => m.MemberID).ToList(); // Order by ID to keep the first one
                    var keepMember = duplicates.First(); // Keep the member with the lowest ID
                    var removeMembers = duplicates.Skip(1).ToList(); // Remove the rest

                    System.Diagnostics.Debug.WriteLine($"Found {duplicates.Count} duplicates for '{keepMember.FirstName} {keepMember.LastName}' (Email: {keepMember.Email}, Contact: {keepMember.ContactNumber}). Keeping ID {keepMember.MemberID}, removing IDs: {string.Join(", ", removeMembers.Select(m => m.MemberID))}");

                    // Update foreign key references before deleting
                    foreach (var duplicate in removeMembers)
                    {
                        try
                        {
                            // Update Attendances that reference this duplicate member
                            var attendancesWithDuplicate = await context.Attendances
                                .Where(a => a.MemberID == duplicate.MemberID)
                                .ToListAsync();
                            
                            foreach (var attendance in attendancesWithDuplicate)
                            {
                                attendance.MemberID = keepMember.MemberID;
                                System.Diagnostics.Debug.WriteLine($"Updated Attendance ID {attendance.AttendanceID} to use Member ID {keepMember.MemberID} instead of {duplicate.MemberID}");
                            }

                            // Update Payments that reference this duplicate member
                            var paymentsWithDuplicate = await context.Payments
                                .Where(p => p.MemberID == duplicate.MemberID)
                                .ToListAsync();
                            
                            foreach (var payment in paymentsWithDuplicate)
                            {
                                payment.MemberID = keepMember.MemberID;
                                System.Diagnostics.Debug.WriteLine($"Updated Payment ID {payment.PaymentID} to use Member ID {keepMember.MemberID} instead of {duplicate.MemberID}");
                            }

                            // Update Notifications that reference this duplicate member
                            var notificationsWithDuplicate = await context.Notifications
                                .Where(n => n.MemberID == duplicate.MemberID)
                                .ToListAsync();
                            
                            foreach (var notification in notificationsWithDuplicate)
                            {
                                notification.MemberID = keepMember.MemberID;
                                System.Diagnostics.Debug.WriteLine($"Updated Notification ID {notification.NotificationID} to use Member ID {keepMember.MemberID} instead of {duplicate.MemberID}");
                            }

                            // Update MemberTrainers that reference this duplicate member
                            var memberTrainersWithDuplicate = await context.MemberTrainers
                                .Where(mt => mt.MemberID == duplicate.MemberID)
                                .ToListAsync();
                            
                            foreach (var memberTrainer in memberTrainersWithDuplicate)
                            {
                                // Check if a MemberTrainer with the same TrainerID and keepMember.MemberID already exists
                                var existingMemberTrainer = await context.MemberTrainers
                                    .FirstOrDefaultAsync(mt => mt.MemberID == keepMember.MemberID && mt.TrainerID == memberTrainer.TrainerID);
                                
                                if (existingMemberTrainer == null)
                                {
                                    // Update to point to the kept member
                                    memberTrainer.MemberID = keepMember.MemberID;
                                    System.Diagnostics.Debug.WriteLine($"Updated MemberTrainer (TrainerID: {memberTrainer.TrainerID}) to use Member ID {keepMember.MemberID} instead of {duplicate.MemberID}");
                                }
                                else
                                {
                                    // Remove duplicate MemberTrainer relationship
                                    context.MemberTrainers.Remove(memberTrainer);
                                    System.Diagnostics.Debug.WriteLine($"Removed duplicate MemberTrainer (MemberID: {duplicate.MemberID}, TrainerID: {memberTrainer.TrainerID})");
                                }
                            }

                            // Update MemberPromos that reference this duplicate member
                            var memberPromosWithDuplicate = await context.MemberPromos
                                .Where(mp => mp.MemberID == duplicate.MemberID)
                                .ToListAsync();
                            
                            foreach (var memberPromo in memberPromosWithDuplicate)
                            {
                                // Check if a MemberPromo with the same PromotionID and keepMember.MemberID already exists
                                var existingMemberPromo = await context.MemberPromos
                                    .FirstOrDefaultAsync(mp => mp.MemberID == keepMember.MemberID && mp.PromotionID == memberPromo.PromotionID);
                                
                                if (existingMemberPromo == null)
                                {
                                    // Update to point to the kept member
                                    memberPromo.MemberID = keepMember.MemberID;
                                    System.Diagnostics.Debug.WriteLine($"Updated MemberPromo (PromotionID: {memberPromo.PromotionID}) to use Member ID {keepMember.MemberID} instead of {duplicate.MemberID}");
                                }
                                else
                                {
                                    // Remove duplicate MemberPromo relationship
                                    context.MemberPromos.Remove(memberPromo);
                                    System.Diagnostics.Debug.WriteLine($"Removed duplicate MemberPromo (MemberID: {duplicate.MemberID}, PromotionID: {memberPromo.PromotionID})");
                                }
                            }

                            // Now delete the duplicate member
                            var memberToDelete = await context.Members.FindAsync(duplicate.MemberID);
                            if (memberToDelete != null)
                            {
                                context.Members.Remove(memberToDelete);
                                duplicatesRemoved++;
                                System.Diagnostics.Debug.WriteLine($"Marked Member ID {duplicate.MemberID} for deletion");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error cleaning up duplicate Member ID {duplicate.MemberID}: {ex.Message}");
                        }
                    }
                }

                // Save all changes
                if (duplicatesRemoved > 0)
                {
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully removed {duplicatesRemoved} duplicate members");
                    result.Message += $" Removed {duplicatesRemoved} duplicate members.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CleanupDuplicateMembers: {ex.Message}");
                // Don't throw - allow sync to continue even if cleanup fails
            }
        }

        private async Task CleanupDuplicatePromotions(AppDbContext context, SyncResult result)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting duplicate promotion cleanup ===");
                
                // Load all promotions (including archived ones)
                var allPromotions = await context.Promotions.AsNoTracking().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {allPromotions.Count} total promotions for duplicate check");
                
                // Count archived promotions
                var archivedCount = allPromotions.Count(p => p.IsArchived);
                System.Diagnostics.Debug.WriteLine($"Found {archivedCount} archived promotions");

                // First, handle duplicate PromoIDs (shouldn't happen but could due to sync issues)
                var promoIdGroups = allPromotions
                    .GroupBy(p => p.PromoID)
                    .Where(g => g.Count() > 1)
                    .ToList();

                int duplicatesRemoved = 0;

                if (promoIdGroups.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Found {promoIdGroups.Count} groups of promotions with duplicate PromoIDs");
                    
                    foreach (var group in promoIdGroups)
                    {
                        var duplicates = group.OrderBy(p => p.PromoID).ToList();
                        var keepPromotion = duplicates.First();
                        var removePromotions = duplicates.Skip(1).ToList();

                        System.Diagnostics.Debug.WriteLine($"Found {duplicates.Count} promotions with same PromoID {keepPromotion.PromoID}. Keeping first, removing others.");

                        foreach (var duplicate in removePromotions)
                        {
                            try
                            {
                                // Update MemberPromos that reference this duplicate promotion
                                var memberPromosWithDuplicate = await context.MemberPromos
                                    .Where(mp => mp.PromotionID == duplicate.PromoID)
                                    .ToListAsync();

                                foreach (var memberPromo in memberPromosWithDuplicate)
                                {
                                    // Check if a MemberPromo with the same MemberID and keepPromotion.PromoID already exists
                                    var existingMemberPromo = await context.MemberPromos
                                        .FirstOrDefaultAsync(mp => mp.MemberID == memberPromo.MemberID && mp.PromotionID == keepPromotion.PromoID);

                                    if (existingMemberPromo == null)
                                    {
                                        memberPromo.PromotionID = keepPromotion.PromoID;
                                        System.Diagnostics.Debug.WriteLine($"Updated MemberPromo (MemberID: {memberPromo.MemberID}) to use Promotion ID {keepPromotion.PromoID} instead of {duplicate.PromoID}");
                                    }
                                    else
                                    {
                                        context.MemberPromos.Remove(memberPromo);
                                        System.Diagnostics.Debug.WriteLine($"Removed duplicate MemberPromo (MemberID: {memberPromo.MemberID}, PromoID: {duplicate.PromoID})");
                                    }
                                }

                                // Delete the duplicate promotion - use direct SQL to ensure it's deleted
                                try
                                {
                                    var promotionToDelete = await context.Promotions.FindAsync(duplicate.PromoID);
                                    if (promotionToDelete != null)
                                    {
                                        context.Promotions.Remove(promotionToDelete);
                                        duplicatesRemoved++;
                                        System.Diagnostics.Debug.WriteLine($"Marked Promotion ID {duplicate.PromoID} for deletion");
                                    }
                                    else
                                    {
                                        // If FindAsync returns null, try direct SQL delete
                                        await context.Database.ExecuteSqlRawAsync(
                                            "DELETE FROM Promotions WHERE PromoID = {0}", duplicate.PromoID);
                                        duplicatesRemoved++;
                                        System.Diagnostics.Debug.WriteLine($"Deleted Promotion ID {duplicate.PromoID} via SQL");
                                    }
                                }
                                catch (Exception deleteEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error deleting Promotion ID {duplicate.PromoID}: {deleteEx.Message}");
                                    // Try direct SQL as fallback
                                    try
                                    {
                                        await context.Database.ExecuteSqlRawAsync(
                                            "DELETE FROM Promotions WHERE PromoID = {0}", duplicate.PromoID);
                                        duplicatesRemoved++;
                                        System.Diagnostics.Debug.WriteLine($"Deleted Promotion ID {duplicate.PromoID} via SQL fallback");
                                    }
                                    catch (Exception sqlEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"SQL delete also failed for Promotion ID {duplicate.PromoID}: {sqlEx.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error cleaning up duplicate Promotion ID {duplicate.PromoID}: {ex.Message}");
                            }
                        }
                    }
                }

                // Save changes from first cleanup before proceeding
                if (duplicatesRemoved > 0)
                {
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Saved {duplicatesRemoved} duplicate promotions removed in first pass");
                }

                // Second, handle true duplicates (same name, dates, and discount rate)
                // Reload after first cleanup to get fresh data
                var allPromotionsAfterIdCleanup = await context.Promotions.AsNoTracking().ToListAsync();
                var trueDuplicateGroups = allPromotionsAfterIdCleanup
                    .GroupBy(p => new
                    {
                        PromoName = (p.PromoName ?? "").Trim().ToLower(),
                        StartDate = p.StartDate.Date,
                        EndDate = p.EndDate.Date,
                        DiscountRate = p.DiscountRate
                    })
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (trueDuplicateGroups.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Found {trueDuplicateGroups.Count} groups of true duplicate promotions");

                    foreach (var group in trueDuplicateGroups)
                    {
                        var duplicates = group.OrderBy(p => p.PromoID).ToList();
                        var keepPromotion = duplicates.First();
                        var removePromotions = duplicates.Skip(1).ToList();

                        System.Diagnostics.Debug.WriteLine($"Found {duplicates.Count} true duplicates for '{keepPromotion.PromoName}'. Keeping ID {keepPromotion.PromoID}, removing IDs: {string.Join(", ", removePromotions.Select(p => p.PromoID))}");

                        foreach (var duplicate in removePromotions)
                        {
                            try
                            {
                                // Update MemberPromos that reference this duplicate promotion
                                var memberPromosWithDuplicate = await context.MemberPromos
                                    .Where(mp => mp.PromotionID == duplicate.PromoID)
                                    .ToListAsync();

                                foreach (var memberPromo in memberPromosWithDuplicate)
                                {
                                    // Check if a MemberPromo with the same MemberID and keepPromotion.PromoID already exists
                                    var existingMemberPromo = await context.MemberPromos
                                        .FirstOrDefaultAsync(mp => mp.MemberID == memberPromo.MemberID && mp.PromotionID == keepPromotion.PromoID);

                                    if (existingMemberPromo == null)
                                    {
                                        memberPromo.PromotionID = keepPromotion.PromoID;
                                        System.Diagnostics.Debug.WriteLine($"Updated MemberPromo (MemberID: {memberPromo.MemberID}) to use Promotion ID {keepPromotion.PromoID} instead of {duplicate.PromoID}");
                                    }
                                    else
                                    {
                                        context.MemberPromos.Remove(memberPromo);
                                        System.Diagnostics.Debug.WriteLine($"Removed duplicate MemberPromo (MemberID: {memberPromo.MemberID}, PromoID: {duplicate.PromoID})");
                                    }
                                }

                                // Delete the duplicate promotion - use direct SQL to ensure it's deleted
                                try
                                {
                                    var promotionToDelete = await context.Promotions.FindAsync(duplicate.PromoID);
                                    if (promotionToDelete != null)
                                    {
                                        context.Promotions.Remove(promotionToDelete);
                                        duplicatesRemoved++;
                                        System.Diagnostics.Debug.WriteLine($"Marked Promotion ID {duplicate.PromoID} for deletion");
                                    }
                                    else
                                    {
                                        // If FindAsync returns null, try direct SQL delete
                                        await context.Database.ExecuteSqlRawAsync(
                                            "DELETE FROM Promotions WHERE PromoID = {0}", duplicate.PromoID);
                                        duplicatesRemoved++;
                                        System.Diagnostics.Debug.WriteLine($"Deleted Promotion ID {duplicate.PromoID} via SQL");
                                    }
                                }
                                catch (Exception deleteEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error deleting Promotion ID {duplicate.PromoID}: {deleteEx.Message}");
                                    // Try direct SQL as fallback
                                    try
                                    {
                                        await context.Database.ExecuteSqlRawAsync(
                                            "DELETE FROM Promotions WHERE PromoID = {0}", duplicate.PromoID);
                                        duplicatesRemoved++;
                                        System.Diagnostics.Debug.WriteLine($"Deleted Promotion ID {duplicate.PromoID} via SQL fallback");
                                    }
                                    catch (Exception sqlEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"SQL delete also failed for Promotion ID {duplicate.PromoID}: {sqlEx.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error cleaning up duplicate Promotion ID {duplicate.PromoID}: {ex.Message}");
                            }
                        }
                    }
                }

                // Save all changes from second cleanup
                if (duplicatesRemoved > 0)
                {
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully removed {duplicatesRemoved} duplicate promotions total");
                    if (result != null)
                    {
                        result.Message += $" Removed {duplicatesRemoved} duplicate promotions.";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No duplicate promotions found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CleanupDuplicatePromotions: {ex.Message}");
                // Don't throw - allow sync to continue even if cleanup fails
            }
        }

        private async Task CleanupDuplicateWalkIns(AppDbContext context, SyncResult result)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting cleanup of duplicate WalkIns...");
                
                // Only process non-archived walk-ins
                var allWalkIns = await context.WalkIns
                    .Where(w => !w.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();
                var duplicatesRemoved = 0;

                // Group by FirstName, LastName, VisitDate (date only), and PaymentAmount to find duplicates
                // Use date only (not time) to catch duplicates that might have different times
                var duplicateGroups = allWalkIns
                    .GroupBy(w => new
                    {
                        FirstName = w.FirstName?.Trim().ToLower() ?? "",
                        LastName = w.LastName?.Trim().ToLower() ?? "",
                        VisitDate = w.VisitDate.Date, // Use date only, ignore time
                        PaymentAmount = w.PaymentAmount
                    })
                    .Where(g => g.Count() > 1)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {duplicateGroups.Count} groups of duplicate walk-ins");

                foreach (var group in duplicateGroups)
                {
                    // Sort by WalkInID (keep the oldest one - lowest ID)
                    var duplicates = group.OrderBy(w => w.WalkInID).ToList();
                    var keepWalkIn = duplicates.First();
                    
                    System.Diagnostics.Debug.WriteLine($"Processing duplicate group: {keepWalkIn.FirstName} {keepWalkIn.LastName} on {keepWalkIn.VisitDate.Date} - {duplicates.Count} duplicates found");
                    
                    // Remove all duplicates except the first one
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        var duplicate = duplicates[i];
                        try
                        {
                            var toRemove = await context.WalkIns.FindAsync(duplicate.WalkInID);
                            if (toRemove != null)
                            {
                                context.WalkIns.Remove(toRemove);
                                duplicatesRemoved++;
                                System.Diagnostics.Debug.WriteLine($"Removing duplicate WalkIn ID {duplicate.WalkInID}: {duplicate.FirstName} {duplicate.LastName} on {duplicate.VisitDate} (keeping ID {keepWalkIn.WalkInID})");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error removing duplicate WalkIn ID {duplicate.WalkInID}: {ex.Message}");
                        }
                    }
                }

                // Save all changes
                if (duplicatesRemoved > 0)
                {
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully removed {duplicatesRemoved} duplicate walk-ins");
                    result.Message += $" Removed {duplicatesRemoved} duplicate walk-ins.";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No duplicate walk-ins found to remove");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CleanupDuplicateWalkIns: {ex.Message}");
                // Don't throw - allow sync to continue even if cleanup fails
            }
        }

        private async Task CleanupDuplicateAttendances(AppDbContext context, SyncResult result)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting cleanup of duplicate Attendances...");
                
                var allAttendances = await context.Attendances.AsNoTracking().ToListAsync();
                var duplicatesRemoved = 0;

                // Group by MemberID and CheckinTime (within 1 second window) to find duplicates
                var duplicateGroups = allAttendances
                    .GroupBy(a => new
                    {
                        MemberID = a.MemberID,
                        // Round CheckinTime to nearest second to group duplicates within 1 second
                        CheckinTimeRounded = new DateTime(
                            a.CheckinTime.Year,
                            a.CheckinTime.Month,
                            a.CheckinTime.Day,
                            a.CheckinTime.Hour,
                            a.CheckinTime.Minute,
                            a.CheckinTime.Second)
                    })
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var group in duplicateGroups)
                {
                    // Sort by AttendanceID (keep the oldest one)
                    var duplicates = group.OrderBy(a => a.AttendanceID).ToList();
                    var keepAttendance = duplicates.First();
                    
                    // Remove all duplicates except the first one
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        var duplicate = duplicates[i];
                        try
                        {
                            var toRemove = await context.Attendances.FindAsync(duplicate.AttendanceID);
                            if (toRemove != null)
                            {
                                context.Attendances.Remove(toRemove);
                                duplicatesRemoved++;
                                System.Diagnostics.Debug.WriteLine($"Removing duplicate Attendance ID {duplicate.AttendanceID}: MemberID={duplicate.MemberID}, CheckinTime={duplicate.CheckinTime:yyyy-MM-dd HH:mm:ss} (keeping AttendanceID={keepAttendance.AttendanceID})");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error removing duplicate Attendance ID {duplicate.AttendanceID}: {ex.Message}");
                        }
                    }
                }

                // Save all changes
                if (duplicatesRemoved > 0)
                {
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully removed {duplicatesRemoved} duplicate attendances");
                    result.Message += $" Removed {duplicatesRemoved} duplicate attendances.";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No duplicate attendances found to clean up.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CleanupDuplicateAttendances: {ex.Message}");
                // Don't throw - allow sync to continue even if cleanup fails
            }
        }

        private bool AreRecordsDifferent<T>(T record1, T record2) where T : class
        {
            if (record1 == null || record2 == null)
                return record1 != record2;

            var entityType = typeof(T);
            var properties = entityType.GetProperties()
                .Where(p => p.CanRead && 
                   !p.Name.Contains("ID") && // Exclude ID properties
                   p.Name != "UserID" && p.Name != "MemberID" && 
                   p.Name != "TrainerID" && p.Name != "RoleID" &&
                   p.Name != "MembershipTypeID" && p.Name != "PromotionID" &&
                   p.Name != "AttendanceID" && p.Name != "PaymentID" &&
                   p.Name != "ScheduleID" && p.Name != "AuditLogID" &&
                   p.Name != "UpdatedAt" && p.Name != "CreatedAt" && // Exclude timestamps for comparison
                   !IsNavigationProperty(p, entityType));

            foreach (var prop in properties)
            {
                var value1 = prop.GetValue(record1);
                var value2 = prop.GetValue(record2);
                
                if (!Equals(value1, value2))
                {
                    return true; // Records are different
                }
            }
            
            return false; // Records are identical
        }
    }
}
