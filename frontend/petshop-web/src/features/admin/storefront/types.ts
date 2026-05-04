export interface BannerSlideResponse {
  id: string;
  imageUrl: string | null;
  title: string | null;
  subtitle: string | null;
  ctaText: string | null;
  ctaType: "none" | "category" | "product" | "external";
  ctaTarget: string | null;
  ctaNewTab: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface StoreFrontConfigResponse {
  id: string;
  primaryColor: string;
  bannerIntervalSecs: number;
  logoUrl: string | null;
  storeName: string | null;
  storeSlogan: string | null;
  announcements: string[];
  slides: BannerSlideResponse[];
  bgColor: string;
  surface2Color: string;
  borderColor: string;
  textColor: string;
  textMutedColor: string;
}

export interface UpdateStoreFrontConfigRequest {
  primaryColor?: string;
  bannerIntervalSecs?: number;
  logoUrl?: string | null;
  storeName?: string | null;
  storeSlogan?: string | null;
  announcements?: string[];
  bgColor?: string;
  surface2Color?: string;
  borderColor?: string;
  textColor?: string;
  textMutedColor?: string;
}

export interface UpsertBannerSlideRequest {
  imageUrl?: string | null;
  title?: string | null;
  subtitle?: string | null;
  ctaText?: string | null;
  ctaType?: string;
  ctaTarget?: string | null;
  ctaNewTab?: boolean;
  sortOrder?: number;
  isActive?: boolean;
}
