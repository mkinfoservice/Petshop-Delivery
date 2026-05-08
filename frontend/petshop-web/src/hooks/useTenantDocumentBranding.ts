import { useEffect } from "react";

type TenantDocumentBranding = {
  logoUrl?: string | null;
  storeName?: string | null;
};

const DEFAULT_TITLE = "vendApps";
const DEFAULT_ICON = "/vite.svg";
const DEFAULT_ICON_TYPE = "image/svg+xml";

function ensureLink(rel: string) {
  const selector = `link[rel="${rel}"]`;
  let link = document.head.querySelector<HTMLLinkElement>(selector);

  if (!link) {
    link = document.createElement("link");
    link.rel = rel;
    document.head.appendChild(link);
  }

  return link;
}

function inferIconType(href: string) {
  const dataType = href.match(/^data:(image\/[^;,]+)/i)?.[1];
  if (dataType) return dataType;

  const clean = href.split("?")[0].toLowerCase();
  if (clean.endsWith(".png")) return "image/png";
  if (clean.endsWith(".jpg") || clean.endsWith(".jpeg")) return "image/jpeg";
  if (clean.endsWith(".webp")) return "image/webp";
  if (clean.endsWith(".ico")) return "image/x-icon";
  if (clean.endsWith(".svg")) return "image/svg+xml";

  return "";
}

export function useTenantDocumentBranding(branding?: TenantDocumentBranding | null) {
  useEffect(() => {
    const storeName = branding?.storeName?.trim() || DEFAULT_TITLE;
    const logoUrl = branding?.logoUrl?.trim() || DEFAULT_ICON;
    const iconType = logoUrl === DEFAULT_ICON ? DEFAULT_ICON_TYPE : inferIconType(logoUrl);

    document.title = storeName;

    const icon = ensureLink("icon");
    icon.href = logoUrl;
    if (iconType) {
      icon.type = iconType;
    } else {
      icon.removeAttribute("type");
    }

    const appleIcon = ensureLink("apple-touch-icon");
    appleIcon.href = logoUrl;
  }, [branding?.logoUrl, branding?.storeName]);
}
