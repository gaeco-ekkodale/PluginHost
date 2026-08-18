// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, { createContext, useContext, useState } from "react";
import { z } from "zod";

// Laufzeitschema für den Kontextwert
const stringSchema = z.string();

// 1. Kontext erstellen
const StringContext = createContext<{
  value: string;
  setValue: (newValue: string) => void;
} | null>(null);

// 2. Provider-Komponente
export const StringProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [value, setValue] = useState<string>("Hello World");

  // Wrapper-Funktion für setValue mit Laufzeitvalidierung
  const safeSetValue = (newValue: string) => {
    const result = stringSchema.safeParse(newValue);
    if (result.error) {
      console.error("Ungültiger Wert für StringContext:", result.error);
      // Optional: Hier könnte man entscheiden, gar nichts zu tun oder einen Fallback setzen.
      return;
    }
    setValue(newValue);
  };

  return (
    <StringContext.Provider value={{ value, setValue: safeSetValue }}>
      {children}
    </StringContext.Provider>
  );
};

// 3. Custom Hook für einfachen Zugriff auf den Kontext
export const useStringContext = () => {
  const context = useContext(StringContext);
  if (!context) {
    throw new Error("useStringContext must be used within a StringProvider");
  }
  return context;
};
