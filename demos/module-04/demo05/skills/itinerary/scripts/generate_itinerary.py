import json
import sys
from pathlib import Path

try:
    from reportlab.lib import colors
    from reportlab.lib.pagesizes import letter
    from reportlab.lib.styles import getSampleStyleSheet
    from reportlab.lib.units import inch
    from reportlab.platypus import SimpleDocTemplate, Spacer, Table, TableStyle, Paragraph
except ImportError:
    print("Install the itinerary PDF dependency with: python -m pip install reportlab", file=sys.stderr)
    raise

input_path = Path(sys.argv[1])
output_path = Path(sys.argv[2])
data = json.loads(input_path.read_text(encoding="utf-8"))
ticket = data["ticket"]
hotel = data["hotel"]
ride = data["ride"]
styles = getSampleStyleSheet()

doc = SimpleDocTemplate(str(output_path), pagesize=letter, rightMargin=0.6 * inch, leftMargin=0.6 * inch)
story = [Paragraph("Concert Travel Itinerary", styles["Title"]), Spacer(1, 0.2 * inch)]
rows = [
    ["Concert", ticket.get("artist", ""), ""],
    ["Venue", ticket.get("venueName", ""), ticket.get("city", "")],
    ["Date", ticket.get("eventDate", ""), ""],
    ["Hotel", hotel.get("hotelName", ""), f"Room {hotel.get('roomId', '')}"],
    ["Room", hotel.get("roomType", ""), f"${hotel.get('pricePerNight', 0)} / night"],
    ["Ride", ride.get("serviceName", ""), ride.get("rideType", "")],
    ["Ride price", "", f"${ride.get('price', 0)}"],
]
table = Table(rows, colWidths=[1.2 * inch, 3.5 * inch, 1.8 * inch])
table.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (0, -1), colors.HexColor("#e6f0f2")),
    ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#8aa4aa")),
    ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ("PADDING", (0, 0), (-1, -1), 8),
]))
story.append(table)
doc.build(story)
print(output_path)
