"""Extract text from a PDF for downstream agent processing."""

import sys
from pathlib import Path

from pypdf import PdfReader


def main() -> None:
    if len(sys.argv) != 2:
        print("Usage: extract_text.py <input.pdf>")
        sys.exit(1)

    pdf_path = Path(sys.argv[1])
    reader = PdfReader(str(pdf_path))
    print(f"Pages: {len(reader.pages)}")

    extracted = 0
    for page_number, page in enumerate(reader.pages, 1):
        text = (page.extract_text() or "").strip()
        if text:
            extracted += len(text)
            print(f"\n--- Page {page_number} ---\n{text}")
        else:
            print(f"\n--- Page {page_number} ---\n[No text layer found]")

    if extracted == 0:
        print("\nNo text was found. The PDF may be image-only; use a rendering/OCR workflow instead.")


if __name__ == "__main__":
    main()
