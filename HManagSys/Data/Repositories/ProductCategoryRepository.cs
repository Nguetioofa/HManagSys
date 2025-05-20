using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;

namespace HManagSys.Data.Repositories
{
    public class ProductCategoryRepository : GenericRepository<ProductCategory>, IProductCategoryRepository
    {

        public ProductCategoryRepository(HospitalManagementContext context,
            ILogger<ProductCategoryRepository> logger,
            IApplicationLogger appLogger)
            : base(context, logger)
        {
        }

   
    }
}
