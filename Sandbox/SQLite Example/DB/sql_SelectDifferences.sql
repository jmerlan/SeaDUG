SELECT p.ElementId, p.Height, p.Width, p.Diameter, p.Length, ap.ElementId, ap.Height, ap.Width, ap.Diameter, ap.Length
FROM Ducts_1499005456 p
JOIN Ducts_1499005601 ap
ON p.ElementId = ap.ElementId
WHERE p.Width != ap.Width OR p.Height != ap.Height