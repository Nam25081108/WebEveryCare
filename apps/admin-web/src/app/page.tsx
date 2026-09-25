import { AdminConsole } from "@/features/dashboard/admin-console";
import { AdminGate } from "@/features/auth/admin-gate";

export default function AdminPage() {
  return <AdminGate><AdminConsole/></AdminGate>;
}
