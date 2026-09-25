import { AdminConsole } from "@/components/admin-console";
import { AdminGate } from "@/components/admin-gate";

export default function AdminPage() {
  return <AdminGate><AdminConsole/></AdminGate>;
}
