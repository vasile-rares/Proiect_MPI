using System.ComponentModel.DataAnnotations;

namespace MonkeyType.Shared.DTOs.Requests.Common
{
    public class PaginationRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 50)]
        public int PageSize { get; set; } = 20;
    }
}
