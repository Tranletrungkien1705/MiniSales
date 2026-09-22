# MiniSales Deepen Log

- 2026-09-22: feat(delivery-order): port nghiệp vụ Quản lý Lệnh giao xe / Lệnh xuất xe từ DMS.Sales (Car_DeliveryOrder + Sto_DlvMinutes) gồm lập lệnh giao xe theo hợp đồng, duyệt lệnh xuất kho, xác nhận bàn giao thực tế và cập nhật trạng thái hợp đồng, hủy lệnh giao xe và thống kê.
- 2026-09-22: feat(dealer-order): port nghiệp vụ Đơn đặt hàng xe Đại lý từ DMS.Sales (Ord_SalesOrderRoot + DMS40_Ord_SalesOrderRootDetail) gồm lập đơn đặt xe (Plan/Unplan), duyệt 2 cấp NPP (Approve1 duyệt số lượng từng dòng, Approve2 chốt kế hoạch), từ chối duyệt, hủy đơn và thống kê.
