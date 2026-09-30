import { AppShell } from "@/components/layout/AppShell";
export default function LayoutInterno({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
