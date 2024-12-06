using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class PortfolioMappings
    {
        public static PortfolioDto ToDTO(this Portfolio p)
        {
            var dto = new PortfolioDto();
            dto.Id = p.Id;
            dto.Positions = new List<PortfolioPositionDto>();
            foreach (var pos in p.Positions)
            {
                var posDto = pos.ToDTO();
                dto.Positions.Add(posDto);
            }
            return dto;
        }
    }
}
