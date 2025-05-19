using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models.EfModels;
using HospitalManagementSystem.Data.Repositories;

namespace HManagSys.Data.Repositories
{
    public class PaymentMethodRepository : GenericRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public PaymentMethodRepository(HospitalManagementContext context, ILogger<PaymentMethodRepository> logger) 
                                    : base(context, logger) {}
    }

}
