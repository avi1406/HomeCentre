using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IPhotoShootManager
    {
        Task<List<MtrProductImageType>> Photoshootstatus();

        Task UpdateInward(long itemcode, DateTime? inwardDate);

        Task UpdateStockReturnDate(long itemcode, DateTime? stockreturndate);

        Task<byte[]> PhotoShootExportExcel(ImageTypeDropdown imageTypeDropdown);
    }
}
