// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

// LayoutContext.tsx
import React, { createContext, useContext, useState, PropsWithChildren } from 'react';

type LayoutContextValue = {
  navItems: React.ReactNode[];
  addNavItem: (item: React.ReactNode) => void;
  footerItems: React.ReactNode[];
  addFooterItem: (item: React.ReactNode) => void;
};

const LayoutContext = createContext<LayoutContextValue>({
  navItems: [
    <a key="home" href="/">
      Home
    </a>,
  ],
  addNavItem: () => {},
  footerItems: [
    <a key="imprint" href="/imprint">
      Imprint
    </a>,
  ],
  addFooterItem: () => {},
});

export function LayoutProvider({ children }: PropsWithChildren) {
  const [navItems, setNavItems] = useState<React.ReactNode[]>([
    <a key="home" href="/">
      Home
    </a>,
  ]);
  const [footerItems, setFooterItems] = useState<React.ReactNode[]>([
    <a key="imprint" href="/imprint">
      Imprint
    </a>,
    <a key="privacy" href="/privacy">
      Privacy
    </a>,
  ]);

  function addNavItem(item: React.ReactNode) {
    setNavItems((prev) => [...prev, item]);
  }

  function addFooterItem(item: React.ReactNode) {
    setFooterItems((prev) => [...prev, item]);
  }

  return (
    <LayoutContext.Provider value={{ navItems, addNavItem, footerItems, addFooterItem }}>
      {children}
    </LayoutContext.Provider>
  );
}

export function useLayout() {
  return useContext(LayoutContext);
}
