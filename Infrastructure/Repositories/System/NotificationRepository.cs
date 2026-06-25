using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext db) : base(db) { }
}
