export interface ServiceOffering {
  key: string;
  title: string;
  summary: string;
  icon: string;
  imageUrl: string;
  highlights: string[];
}

export interface Testimonial {
  quote: string;
  author: string;
  project: string;
}

export interface GalleryImage {
  imageUrl: string;
  label: string;
  alt: string;
}

export interface SiteInfo {
  businessName: string;
  tagline: string;
  phone: string;
  email: string;
  address: string;
  serviceArea: string;
  hours: string;
  facebookUrl: string;
  messengerUrl: string;
  yearsInBusiness: number;
  heroImageUrl: string;
  services: ServiceOffering[];
  testimonials: Testimonial[];
  gallery: GalleryImage[];
}

export interface Promotion {
  id: string;
  title: string;
  summary: string;
  badge: string | null;
  imageUrl: string | null;
  ctaText: string | null;
  ctaUrl: string | null;
  isFeatured: boolean;
  isActive: boolean;
  sortOrder: number;
  startsOn: string | null;
  endsOn: string | null;
}

export interface ContactRequest {
  name: string;
  email: string;
  phone: string;
  service: string;
  address: string;
  message: string;
  website: string;
}
