import { Footer } from "@/components/layout/footer";
import { MarketplaceHome } from "@/features/catalog/components/marketplace-home";
import { SiteHeader } from "@/components/layout/site-header";

export default function HomePage(){
  return <main><SiteHeader/><MarketplaceHome/><Footer/></main>;
}
