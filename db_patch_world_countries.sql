-- Patch: replace the small starter country list with the full list of
-- world countries (~195), so the Country dropdown is never limited.
-- Cities remain free-text in the IMF form (seeding every city on earth
-- is not practical), but Country is now exhaustive.

DELETE FROM Pays WHERE Code NOT IN (SELECT Code FROM (SELECT Code FROM Pays) x); -- no-op guard, keep existing rows

INSERT INTO Pays (Code, Nom)
SELECT v.Code, v.Nom FROM (VALUES
('AF','Afghanistan'),('ZA','Afrique du Sud'),('AL','Albanie'),('DZ','Algerie'),('DE','Allemagne'),
('AD','Andorre'),('AO','Angola'),('AG','Antigua-et-Barbuda'),('SA','Arabie saoudite'),('AR','Argentine'),
('AM','Armenie'),('AU','Australie'),('AT','Autriche'),('AZ','Azerbaidjan'),('BS','Bahamas'),
('BH','Bahrein'),('BD','Bangladesh'),('BB','Barbade'),('BE','Belgique'),('BZ','Belize'),
('BJ','Benin'),('BT','Bhoutan'),('BY','Bielorussie'),('MM','Birmanie'),('BO','Bolivie'),
('BA','Bosnie-Herzegovine'),('BW','Botswana'),('BR','Bresil'),('BN','Brunei'),('BG','Bulgarie'),
('BF','Burkina Faso'),('BI','Burundi'),('KH','Cambodge'),('CM','Cameroun'),('CA','Canada'),
('CV','Cap-Vert'),('CL','Chili'),('CN','Chine'),('CY','Chypre'),('CO','Colombie'),
('KM','Comores'),('CG','Congo'),('CD','Republique Democratique du Congo'),('KR','Coree du Sud'),
('KP','Coree du Nord'),('CR','Costa Rica'),('CI','Cote d''Ivoire'),('HR','Croatie'),('CU','Cuba'),
('DK','Danemark'),('DJ','Djibouti'),('DO','Republique Dominicaine'),('EG','Egypte'),
('AE','Emirats Arabes Unis'),('EC','Equateur'),('ER','Erythree'),('ES','Espagne'),('EE','Estonie'),
('SZ','Eswatini'),('US','Etats-Unis'),('ET','Ethiopie'),('FJ','Fidji'),('FI','Finlande'),
('FR','France'),('GA','Gabon'),('GM','Gambie'),('GE','Georgie'),('GH','Ghana'),('GR','Grece'),
('GD','Grenade'),('GT','Guatemala'),('GN','Guinee'),('GQ','Guinee equatoriale'),
('GW','Guinee-Bissau'),('GY','Guyana'),('HT','Haiti'),('HN','Honduras'),('HU','Hongrie'),
('IN','Inde'),('ID','Indonesie'),('IQ','Irak'),('IR','Iran'),('IE','Irlande'),('IS','Islande'),
('IL','Israel'),('IT','Italie'),('JM','Jamaique'),('JP','Japon'),('JO','Jordanie'),
('KZ','Kazakhstan'),('KE','Kenya'),('KG','Kirghizistan'),('KI','Kiribati'),('KW','Koweit'),
('LA','Laos'),('LS','Lesotho'),('LV','Lettonie'),('LB','Liban'),('LR','Liberia'),('LY','Libye'),
('LI','Liechtenstein'),('LT','Lituanie'),('LU','Luxembourg'),('MK','Macedoine du Nord'),
('MG','Madagascar'),('MY','Malaisie'),('MW','Malawi'),('MV','Maldives'),('ML','Mali'),
('MT','Malte'),('MA','Maroc'),('MH','Iles Marshall'),('MU','Maurice'),('MR','Mauritanie'),
('MX','Mexique'),('FM','Micronesie'),('MD','Moldavie'),('MC','Monaco'),('MN','Mongolie'),
('ME','Montenegro'),('MZ','Mozambique'),('NA','Namibie'),('NR','Nauru'),('NP','Nepal'),
('NI','Nicaragua'),('NE','Niger'),('NG','Nigeria'),('NO','Norvege'),('NZ','Nouvelle-Zelande'),
('OM','Oman'),('UG','Ouganda'),('UZ','Ouzbekistan'),('PK','Pakistan'),('PW','Palaos'),
('PA','Panama'),('PG','Papouasie-Nouvelle-Guinee'),('PY','Paraguay'),('NL','Pays-Bas'),
('PE','Perou'),('PH','Philippines'),('PL','Pologne'),('PT','Portugal'),('QA','Qatar'),
('RO','Roumanie'),('GB','Royaume-Uni'),('RU','Russie'),('RW','Rwanda'),
('KN','Saint-Christophe-et-Nieves'),('SM','Saint-Marin'),('VC','Saint-Vincent-et-les-Grenadines'),
('LC','Sainte-Lucie'),('SB','Iles Salomon'),('SV','Salvador'),('WS','Samoa'),
('ST','Sao Tome-et-Principe'),('SN','Senegal'),('RS','Serbie'),('SC','Seychelles'),
('SL','Sierra Leone'),('SG','Singapour'),('SK','Slovaquie'),('SI','Slovenie'),('SO','Somalie'),
('SD','Soudan'),('SS','Soudan du Sud'),('LK','Sri Lanka'),('SE','Suede'),('CH','Suisse'),
('SR','Suriname'),('SY','Syrie'),('TJ','Tadjikistan'),('TZ','Tanzanie'),('TD','Tchad'),
('CZ','Republique Tcheque'),('TH','Thailande'),('TL','Timor Oriental'),('TG','Togo'),
('TO','Tonga'),('TT','Trinite-et-Tobago'),('TN','Tunisie'),('TM','Turkmenistan'),
('TR','Turquie'),('TV','Tuvalu'),('UA','Ukraine'),('UY','Uruguay'),('VU','Vanuatu'),
('VA','Vatican'),('VE','Venezuela'),('VN','Vietnam'),('YE','Yemen'),('ZM','Zambie'),('ZW','Zimbabwe')
) AS v(Code, Nom)
WHERE NOT EXISTS (SELECT 1 FROM Pays p WHERE p.Code = v.Code);
GO

-- City becomes free text (seeding every city on earth isn't practical);
-- VilleID stays available for other modules that do use the seeded Ville list.
ALTER TABLE IMF ADD CityName NVARCHAR(100) NULL;
ALTER TABLE IMFTmp ADD CityName NVARCHAR(100) NULL;
GO
