using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.DotNet.Converter.Database
{
    public static class SqlServerConventions
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            // ตัวอย่าง: กำหนด Schema เริ่มต้น
            modelBuilder.HasDefaultSchema("dbo");

            // Example: กำหนด MaxLength อัตโนมัติ
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                        property.SetMaxLength(255);
                }
            }

            // (Optional) ตั้งค่าส่วนอื่นตามต้องการ
        }
    }
}
