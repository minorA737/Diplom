using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.Services
{
    public class ProductionService
    {
        private readonly PostgresContext _dbContext;

        public ProductionService(PostgresContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Получение списка всех заказ-нарядов
        public async Task<List<ProductionDetail>> GetAllProductionOrdersAsync()
        {
            return await _dbContext.ProductionDetails
                .Include(p => p.OrderPosition)
                .ThenInclude(op => op.Order)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        // Получение заказ-наряда по ID
        public async Task<ProductionDetail> GetProductionOrderByIdAsync(int id)
        {
            return await _dbContext.ProductionDetails
                .Include(p => p.OrderPosition)
                .ThenInclude(op => op.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Создание нового заказ-наряда
        public async Task<bool> CreateProductionOrderAsync(ProductionDetail productionOrder)
        {
            try
            {
                _dbContext.ProductionDetails.Add(productionOrder);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Обновление существующего заказ-наряда
        public async Task<bool> UpdateProductionOrderAsync(ProductionDetail productionOrder)
        {
            try
            {
                _dbContext.ProductionDetails.Update(productionOrder);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Удаление заказ-наряда
        public async Task<bool> DeleteProductionOrderAsync(int id)
        {
            try
            {
                var order = await _dbContext.ProductionDetails.FindAsync(id);
                if (order != null)
                {
                    _dbContext.ProductionDetails.Remove(order);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}