-- Patch: replace the small starter geo seed with a worldwide dataset
-- (~190 countries + their capital city), and expand Currency/TimeZone
-- to cover the whole world, not just a handful of entries.
--
-- SAFE TO RUN even after db_patch_geo_lookups.sql: it clears the small
-- starter seed first (Ville -> Region -> Pays, in FK-safe order) before
-- inserting the full dataset, so you won't get duplicate countries.
-- Only run the cleanup if no Agence/IMF row already references a
-- PaysID/VilleID from the old starter seed (safe at this stage of testing).

DELETE FROM Ville;
DELETE FROM Region;
DELETE FROM Pays;
GO

-- Broad world seed: ~190 countries with their capital city
INSERT INTO Pays (Code, Nom) VALUES
('AF','Afghanistan'),
('ZA','Afrique du Sud'),
('AL','Albanie'),
('DZ','Algerie'),
('DE','Allemagne'),
('AD','Andorre'),
('AO','Angola'),
('SA','Arabie Saoudite'),
('AR','Argentine'),
('AM','Armenie'),
('AU','Australie'),
('AT','Autriche'),
('AZ','Azerbaidjan'),
('BS','Bahamas'),
('BH','Bahrein'),
('BD','Bangladesh'),
('BE','Belgique'),
('BZ','Belize'),
('BJ','Benin'),
('BT','Bhoutan'),
('BY','Bielorussie'),
('MM','Birmanie'),
('BO','Bolivie'),
('BA','Bosnie-Herzegovine'),
('BW','Botswana'),
('BR','Bresil'),
('BN','Brunei'),
('BG','Bulgarie'),
('BF','Burkina Faso'),
('BI','Burundi'),
('KH','Cambodge'),
('CM','Cameroun'),
('CA','Canada'),
('CV','Cap-Vert'),
('CF','Republique Centrafricaine'),
('CL','Chili'),
('CN','Chine'),
('CY','Chypre'),
('CO','Colombie'),
('KM','Comores'),
('CG','Congo'),
('CD','RD Congo'),
('KR','Coree du Sud'),
('KP','Coree du Nord'),
('CR','Costa Rica'),
('CI','Cote d''Ivoire'),
('HR','Croatie'),
('CU','Cuba'),
('DK','Danemark'),
('DJ','Djibouti'),
('EG','Egypte'),
('AE','Emirats Arabes Unis'),
('EC','Equateur'),
('ER','Erythree'),
('ES','Espagne'),
('EE','Estonie'),
('SZ','Eswatini'),
('US','Etats-Unis'),
('ET','Ethiopie'),
('FJ','Fidji'),
('FI','Finlande'),
('FR','France'),
('GA','Gabon'),
('GM','Gambie'),
('GE','Georgie'),
('GH','Ghana'),
('GR','Grece'),
('GD','Grenade'),
('GT','Guatemala'),
('GN','Guinee'),
('GW','Guinee-Bissau'),
('GQ','Guinee Equatoriale'),
('GY','Guyana'),
('HT','Haiti'),
('HN','Honduras'),
('HU','Hongrie'),
('IN','Inde'),
('ID','Indonesie'),
('IQ','Irak'),
('IR','Iran'),
('IE','Irlande'),
('IS','Islande'),
('IL','Israel'),
('IT','Italie'),
('JM','Jamaique'),
('JP','Japon'),
('JO','Jordanie'),
('KZ','Kazakhstan'),
('KE','Kenya'),
('KG','Kirghizistan'),
('KI','Kiribati'),
('KW','Koweit'),
('LA','Laos'),
('LS','Lesotho'),
('LV','Lettonie'),
('LB','Liban'),
('LR','Liberia'),
('LY','Libye'),
('LI','Liechtenstein'),
('LT','Lituanie'),
('LU','Luxembourg'),
('MK','Macedoine du Nord'),
('MG','Madagascar'),
('MY','Malaisie'),
('MW','Malawi'),
('MV','Maldives'),
('ML','Mali'),
('MT','Malte'),
('MA','Maroc'),
('MH','Iles Marshall'),
('MU','Maurice'),
('MR','Mauritanie'),
('MX','Mexique'),
('FM','Micronesie'),
('MD','Moldavie'),
('MC','Monaco'),
('MN','Mongolie'),
('ME','Montenegro'),
('MZ','Mozambique'),
('NA','Namibie'),
('NR','Nauru'),
('NP','Nepal'),
('NI','Nicaragua'),
('NE','Niger'),
('NG','Nigeria'),
('NO','Norvege'),
('NZ','Nouvelle-Zelande'),
('OM','Oman'),
('UG','Ouganda'),
('UZ','Ouzbekistan'),
('PK','Pakistan'),
('PW','Palaos'),
('PA','Panama'),
('PG','Papouasie-Nouvelle-Guinee'),
('PY','Paraguay'),
('NL','Pays-Bas'),
('PE','Perou'),
('PH','Philippines'),
('PL','Pologne'),
('PT','Portugal'),
('QA','Qatar'),
('RO','Roumanie'),
('GB','Royaume-Uni'),
('RU','Russie'),
('RW','Rwanda'),
('KN','Saint-Christophe-et-Nieves'),
('LC','Sainte-Lucie'),
('SM','Saint-Marin'),
('VC','Saint-Vincent-et-les-Grenadines'),
('SB','Iles Salomon'),
('SV','Salvador'),
('WS','Samoa'),
('ST','Sao Tome-et-Principe'),
('SN','Senegal'),
('RS','Serbie'),
('SC','Seychelles'),
('SL','Sierra Leone'),
('SG','Singapour'),
('SK','Slovaquie'),
('SI','Slovenie'),
('SO','Somalie'),
('SD','Soudan'),
('SS','Soudan du Sud'),
('LK','Sri Lanka'),
('SE','Suede'),
('CH','Suisse'),
('SR','Suriname'),
('SY','Syrie'),
('TJ','Tadjikistan'),
('TZ','Tanzanie'),
('TD','Tchad'),
('CZ','Tchequie'),
('TH','Thailande'),
('TL','Timor Oriental'),
('TG','Togo'),
('TO','Tonga'),
('TT','Trinite-et-Tobago'),
('TN','Tunisie'),
('TM','Turkmenistan'),
('TR','Turquie'),
('TV','Tuvalu'),
('UA','Ukraine'),
('UY','Uruguay'),
('VU','Vanuatu'),
('VA','Vatican'),
('VE','Venezuela'),
('VN','Vietnam'),
('YE','Yemen'),
('ZM','Zambie'),
('ZW','Zimbabwe');
GO

-- One Region per country (needed because Ville -> Region -> Pays)
INSERT INTO Region (Nom, PaysID)
SELECT 'Region Afghanistan', PaysID FROM Pays WHERE Code = 'AF'
UNION ALL SELECT 'Region Afrique du Sud', PaysID FROM Pays WHERE Code = 'ZA'
UNION ALL SELECT 'Region Albanie', PaysID FROM Pays WHERE Code = 'AL'
UNION ALL SELECT 'Region Algerie', PaysID FROM Pays WHERE Code = 'DZ'
UNION ALL SELECT 'Region Allemagne', PaysID FROM Pays WHERE Code = 'DE'
UNION ALL SELECT 'Region Andorre', PaysID FROM Pays WHERE Code = 'AD'
UNION ALL SELECT 'Region Angola', PaysID FROM Pays WHERE Code = 'AO'
UNION ALL SELECT 'Region Arabie Saoudite', PaysID FROM Pays WHERE Code = 'SA'
UNION ALL SELECT 'Region Argentine', PaysID FROM Pays WHERE Code = 'AR'
UNION ALL SELECT 'Region Armenie', PaysID FROM Pays WHERE Code = 'AM'
UNION ALL SELECT 'Region Australie', PaysID FROM Pays WHERE Code = 'AU'
UNION ALL SELECT 'Region Autriche', PaysID FROM Pays WHERE Code = 'AT'
UNION ALL SELECT 'Region Azerbaidjan', PaysID FROM Pays WHERE Code = 'AZ'
UNION ALL SELECT 'Region Bahamas', PaysID FROM Pays WHERE Code = 'BS'
UNION ALL SELECT 'Region Bahrein', PaysID FROM Pays WHERE Code = 'BH'
UNION ALL SELECT 'Region Bangladesh', PaysID FROM Pays WHERE Code = 'BD'
UNION ALL SELECT 'Region Belgique', PaysID FROM Pays WHERE Code = 'BE'
UNION ALL SELECT 'Region Belize', PaysID FROM Pays WHERE Code = 'BZ'
UNION ALL SELECT 'Region Benin', PaysID FROM Pays WHERE Code = 'BJ'
UNION ALL SELECT 'Region Bhoutan', PaysID FROM Pays WHERE Code = 'BT'
UNION ALL SELECT 'Region Bielorussie', PaysID FROM Pays WHERE Code = 'BY'
UNION ALL SELECT 'Region Birmanie', PaysID FROM Pays WHERE Code = 'MM'
UNION ALL SELECT 'Region Bolivie', PaysID FROM Pays WHERE Code = 'BO'
UNION ALL SELECT 'Region Bosnie-Herzegovine', PaysID FROM Pays WHERE Code = 'BA'
UNION ALL SELECT 'Region Botswana', PaysID FROM Pays WHERE Code = 'BW'
UNION ALL SELECT 'Region Bresil', PaysID FROM Pays WHERE Code = 'BR'
UNION ALL SELECT 'Region Brunei', PaysID FROM Pays WHERE Code = 'BN'
UNION ALL SELECT 'Region Bulgarie', PaysID FROM Pays WHERE Code = 'BG'
UNION ALL SELECT 'Region Burkina Faso', PaysID FROM Pays WHERE Code = 'BF'
UNION ALL SELECT 'Region Burundi', PaysID FROM Pays WHERE Code = 'BI'
UNION ALL SELECT 'Region Cambodge', PaysID FROM Pays WHERE Code = 'KH'
UNION ALL SELECT 'Region Cameroun', PaysID FROM Pays WHERE Code = 'CM'
UNION ALL SELECT 'Region Canada', PaysID FROM Pays WHERE Code = 'CA'
UNION ALL SELECT 'Region Cap-Vert', PaysID FROM Pays WHERE Code = 'CV'
UNION ALL SELECT 'Region Republique Centrafricaine', PaysID FROM Pays WHERE Code = 'CF'
UNION ALL SELECT 'Region Chili', PaysID FROM Pays WHERE Code = 'CL'
UNION ALL SELECT 'Region Chine', PaysID FROM Pays WHERE Code = 'CN'
UNION ALL SELECT 'Region Chypre', PaysID FROM Pays WHERE Code = 'CY'
UNION ALL SELECT 'Region Colombie', PaysID FROM Pays WHERE Code = 'CO'
UNION ALL SELECT 'Region Comores', PaysID FROM Pays WHERE Code = 'KM'
UNION ALL SELECT 'Region Congo', PaysID FROM Pays WHERE Code = 'CG'
UNION ALL SELECT 'Region RD Congo', PaysID FROM Pays WHERE Code = 'CD'
UNION ALL SELECT 'Region Coree du Sud', PaysID FROM Pays WHERE Code = 'KR'
UNION ALL SELECT 'Region Coree du Nord', PaysID FROM Pays WHERE Code = 'KP'
UNION ALL SELECT 'Region Costa Rica', PaysID FROM Pays WHERE Code = 'CR'
UNION ALL SELECT 'Region Cote d''Ivoire', PaysID FROM Pays WHERE Code = 'CI'
UNION ALL SELECT 'Region Croatie', PaysID FROM Pays WHERE Code = 'HR'
UNION ALL SELECT 'Region Cuba', PaysID FROM Pays WHERE Code = 'CU'
UNION ALL SELECT 'Region Danemark', PaysID FROM Pays WHERE Code = 'DK'
UNION ALL SELECT 'Region Djibouti', PaysID FROM Pays WHERE Code = 'DJ'
UNION ALL SELECT 'Region Egypte', PaysID FROM Pays WHERE Code = 'EG'
UNION ALL SELECT 'Region Emirats Arabes Unis', PaysID FROM Pays WHERE Code = 'AE'
UNION ALL SELECT 'Region Equateur', PaysID FROM Pays WHERE Code = 'EC'
UNION ALL SELECT 'Region Erythree', PaysID FROM Pays WHERE Code = 'ER'
UNION ALL SELECT 'Region Espagne', PaysID FROM Pays WHERE Code = 'ES'
UNION ALL SELECT 'Region Estonie', PaysID FROM Pays WHERE Code = 'EE'
UNION ALL SELECT 'Region Eswatini', PaysID FROM Pays WHERE Code = 'SZ'
UNION ALL SELECT 'Region Etats-Unis', PaysID FROM Pays WHERE Code = 'US'
UNION ALL SELECT 'Region Ethiopie', PaysID FROM Pays WHERE Code = 'ET'
UNION ALL SELECT 'Region Fidji', PaysID FROM Pays WHERE Code = 'FJ'
UNION ALL SELECT 'Region Finlande', PaysID FROM Pays WHERE Code = 'FI'
UNION ALL SELECT 'Region France', PaysID FROM Pays WHERE Code = 'FR'
UNION ALL SELECT 'Region Gabon', PaysID FROM Pays WHERE Code = 'GA'
UNION ALL SELECT 'Region Gambie', PaysID FROM Pays WHERE Code = 'GM'
UNION ALL SELECT 'Region Georgie', PaysID FROM Pays WHERE Code = 'GE'
UNION ALL SELECT 'Region Ghana', PaysID FROM Pays WHERE Code = 'GH'
UNION ALL SELECT 'Region Grece', PaysID FROM Pays WHERE Code = 'GR'
UNION ALL SELECT 'Region Grenade', PaysID FROM Pays WHERE Code = 'GD'
UNION ALL SELECT 'Region Guatemala', PaysID FROM Pays WHERE Code = 'GT'
UNION ALL SELECT 'Region Guinee', PaysID FROM Pays WHERE Code = 'GN'
UNION ALL SELECT 'Region Guinee-Bissau', PaysID FROM Pays WHERE Code = 'GW'
UNION ALL SELECT 'Region Guinee Equatoriale', PaysID FROM Pays WHERE Code = 'GQ'
UNION ALL SELECT 'Region Guyana', PaysID FROM Pays WHERE Code = 'GY'
UNION ALL SELECT 'Region Haiti', PaysID FROM Pays WHERE Code = 'HT'
UNION ALL SELECT 'Region Honduras', PaysID FROM Pays WHERE Code = 'HN'
UNION ALL SELECT 'Region Hongrie', PaysID FROM Pays WHERE Code = 'HU'
UNION ALL SELECT 'Region Inde', PaysID FROM Pays WHERE Code = 'IN'
UNION ALL SELECT 'Region Indonesie', PaysID FROM Pays WHERE Code = 'ID'
UNION ALL SELECT 'Region Irak', PaysID FROM Pays WHERE Code = 'IQ'
UNION ALL SELECT 'Region Iran', PaysID FROM Pays WHERE Code = 'IR'
UNION ALL SELECT 'Region Irlande', PaysID FROM Pays WHERE Code = 'IE'
UNION ALL SELECT 'Region Islande', PaysID FROM Pays WHERE Code = 'IS'
UNION ALL SELECT 'Region Israel', PaysID FROM Pays WHERE Code = 'IL'
UNION ALL SELECT 'Region Italie', PaysID FROM Pays WHERE Code = 'IT'
UNION ALL SELECT 'Region Jamaique', PaysID FROM Pays WHERE Code = 'JM'
UNION ALL SELECT 'Region Japon', PaysID FROM Pays WHERE Code = 'JP'
UNION ALL SELECT 'Region Jordanie', PaysID FROM Pays WHERE Code = 'JO'
UNION ALL SELECT 'Region Kazakhstan', PaysID FROM Pays WHERE Code = 'KZ'
UNION ALL SELECT 'Region Kenya', PaysID FROM Pays WHERE Code = 'KE'
UNION ALL SELECT 'Region Kirghizistan', PaysID FROM Pays WHERE Code = 'KG'
UNION ALL SELECT 'Region Kiribati', PaysID FROM Pays WHERE Code = 'KI'
UNION ALL SELECT 'Region Koweit', PaysID FROM Pays WHERE Code = 'KW'
UNION ALL SELECT 'Region Laos', PaysID FROM Pays WHERE Code = 'LA'
UNION ALL SELECT 'Region Lesotho', PaysID FROM Pays WHERE Code = 'LS'
UNION ALL SELECT 'Region Lettonie', PaysID FROM Pays WHERE Code = 'LV'
UNION ALL SELECT 'Region Liban', PaysID FROM Pays WHERE Code = 'LB'
UNION ALL SELECT 'Region Liberia', PaysID FROM Pays WHERE Code = 'LR'
UNION ALL SELECT 'Region Libye', PaysID FROM Pays WHERE Code = 'LY'
UNION ALL SELECT 'Region Liechtenstein', PaysID FROM Pays WHERE Code = 'LI'
UNION ALL SELECT 'Region Lituanie', PaysID FROM Pays WHERE Code = 'LT'
UNION ALL SELECT 'Region Luxembourg', PaysID FROM Pays WHERE Code = 'LU'
UNION ALL SELECT 'Region Macedoine du Nord', PaysID FROM Pays WHERE Code = 'MK'
UNION ALL SELECT 'Region Madagascar', PaysID FROM Pays WHERE Code = 'MG'
UNION ALL SELECT 'Region Malaisie', PaysID FROM Pays WHERE Code = 'MY'
UNION ALL SELECT 'Region Malawi', PaysID FROM Pays WHERE Code = 'MW'
UNION ALL SELECT 'Region Maldives', PaysID FROM Pays WHERE Code = 'MV'
UNION ALL SELECT 'Region Mali', PaysID FROM Pays WHERE Code = 'ML'
UNION ALL SELECT 'Region Malte', PaysID FROM Pays WHERE Code = 'MT'
UNION ALL SELECT 'Region Maroc', PaysID FROM Pays WHERE Code = 'MA'
UNION ALL SELECT 'Region Iles Marshall', PaysID FROM Pays WHERE Code = 'MH'
UNION ALL SELECT 'Region Maurice', PaysID FROM Pays WHERE Code = 'MU'
UNION ALL SELECT 'Region Mauritanie', PaysID FROM Pays WHERE Code = 'MR'
UNION ALL SELECT 'Region Mexique', PaysID FROM Pays WHERE Code = 'MX'
UNION ALL SELECT 'Region Micronesie', PaysID FROM Pays WHERE Code = 'FM'
UNION ALL SELECT 'Region Moldavie', PaysID FROM Pays WHERE Code = 'MD'
UNION ALL SELECT 'Region Monaco', PaysID FROM Pays WHERE Code = 'MC'
UNION ALL SELECT 'Region Mongolie', PaysID FROM Pays WHERE Code = 'MN'
UNION ALL SELECT 'Region Montenegro', PaysID FROM Pays WHERE Code = 'ME'
UNION ALL SELECT 'Region Mozambique', PaysID FROM Pays WHERE Code = 'MZ'
UNION ALL SELECT 'Region Namibie', PaysID FROM Pays WHERE Code = 'NA'
UNION ALL SELECT 'Region Nauru', PaysID FROM Pays WHERE Code = 'NR'
UNION ALL SELECT 'Region Nepal', PaysID FROM Pays WHERE Code = 'NP'
UNION ALL SELECT 'Region Nicaragua', PaysID FROM Pays WHERE Code = 'NI'
UNION ALL SELECT 'Region Niger', PaysID FROM Pays WHERE Code = 'NE'
UNION ALL SELECT 'Region Nigeria', PaysID FROM Pays WHERE Code = 'NG'
UNION ALL SELECT 'Region Norvege', PaysID FROM Pays WHERE Code = 'NO'
UNION ALL SELECT 'Region Nouvelle-Zelande', PaysID FROM Pays WHERE Code = 'NZ'
UNION ALL SELECT 'Region Oman', PaysID FROM Pays WHERE Code = 'OM'
UNION ALL SELECT 'Region Ouganda', PaysID FROM Pays WHERE Code = 'UG'
UNION ALL SELECT 'Region Ouzbekistan', PaysID FROM Pays WHERE Code = 'UZ'
UNION ALL SELECT 'Region Pakistan', PaysID FROM Pays WHERE Code = 'PK'
UNION ALL SELECT 'Region Palaos', PaysID FROM Pays WHERE Code = 'PW'
UNION ALL SELECT 'Region Panama', PaysID FROM Pays WHERE Code = 'PA'
UNION ALL SELECT 'Region Papouasie-Nouvelle-Guinee', PaysID FROM Pays WHERE Code = 'PG'
UNION ALL SELECT 'Region Paraguay', PaysID FROM Pays WHERE Code = 'PY'
UNION ALL SELECT 'Region Pays-Bas', PaysID FROM Pays WHERE Code = 'NL'
UNION ALL SELECT 'Region Perou', PaysID FROM Pays WHERE Code = 'PE'
UNION ALL SELECT 'Region Philippines', PaysID FROM Pays WHERE Code = 'PH'
UNION ALL SELECT 'Region Pologne', PaysID FROM Pays WHERE Code = 'PL'
UNION ALL SELECT 'Region Portugal', PaysID FROM Pays WHERE Code = 'PT'
UNION ALL SELECT 'Region Qatar', PaysID FROM Pays WHERE Code = 'QA'
UNION ALL SELECT 'Region Roumanie', PaysID FROM Pays WHERE Code = 'RO'
UNION ALL SELECT 'Region Royaume-Uni', PaysID FROM Pays WHERE Code = 'GB'
UNION ALL SELECT 'Region Russie', PaysID FROM Pays WHERE Code = 'RU'
UNION ALL SELECT 'Region Rwanda', PaysID FROM Pays WHERE Code = 'RW'
UNION ALL SELECT 'Region Saint-Christophe-et-Nieves', PaysID FROM Pays WHERE Code = 'KN'
UNION ALL SELECT 'Region Sainte-Lucie', PaysID FROM Pays WHERE Code = 'LC'
UNION ALL SELECT 'Region Saint-Marin', PaysID FROM Pays WHERE Code = 'SM'
UNION ALL SELECT 'Region Saint-Vincent-et-les-Grenadines', PaysID FROM Pays WHERE Code = 'VC'
UNION ALL SELECT 'Region Iles Salomon', PaysID FROM Pays WHERE Code = 'SB'
UNION ALL SELECT 'Region Salvador', PaysID FROM Pays WHERE Code = 'SV'
UNION ALL SELECT 'Region Samoa', PaysID FROM Pays WHERE Code = 'WS'
UNION ALL SELECT 'Region Sao Tome-et-Principe', PaysID FROM Pays WHERE Code = 'ST'
UNION ALL SELECT 'Region Senegal', PaysID FROM Pays WHERE Code = 'SN'
UNION ALL SELECT 'Region Serbie', PaysID FROM Pays WHERE Code = 'RS'
UNION ALL SELECT 'Region Seychelles', PaysID FROM Pays WHERE Code = 'SC'
UNION ALL SELECT 'Region Sierra Leone', PaysID FROM Pays WHERE Code = 'SL'
UNION ALL SELECT 'Region Singapour', PaysID FROM Pays WHERE Code = 'SG'
UNION ALL SELECT 'Region Slovaquie', PaysID FROM Pays WHERE Code = 'SK'
UNION ALL SELECT 'Region Slovenie', PaysID FROM Pays WHERE Code = 'SI'
UNION ALL SELECT 'Region Somalie', PaysID FROM Pays WHERE Code = 'SO'
UNION ALL SELECT 'Region Soudan', PaysID FROM Pays WHERE Code = 'SD'
UNION ALL SELECT 'Region Soudan du Sud', PaysID FROM Pays WHERE Code = 'SS'
UNION ALL SELECT 'Region Sri Lanka', PaysID FROM Pays WHERE Code = 'LK'
UNION ALL SELECT 'Region Suede', PaysID FROM Pays WHERE Code = 'SE'
UNION ALL SELECT 'Region Suisse', PaysID FROM Pays WHERE Code = 'CH'
UNION ALL SELECT 'Region Suriname', PaysID FROM Pays WHERE Code = 'SR'
UNION ALL SELECT 'Region Syrie', PaysID FROM Pays WHERE Code = 'SY'
UNION ALL SELECT 'Region Tadjikistan', PaysID FROM Pays WHERE Code = 'TJ'
UNION ALL SELECT 'Region Tanzanie', PaysID FROM Pays WHERE Code = 'TZ'
UNION ALL SELECT 'Region Tchad', PaysID FROM Pays WHERE Code = 'TD'
UNION ALL SELECT 'Region Tchequie', PaysID FROM Pays WHERE Code = 'CZ'
UNION ALL SELECT 'Region Thailande', PaysID FROM Pays WHERE Code = 'TH'
UNION ALL SELECT 'Region Timor Oriental', PaysID FROM Pays WHERE Code = 'TL'
UNION ALL SELECT 'Region Togo', PaysID FROM Pays WHERE Code = 'TG'
UNION ALL SELECT 'Region Tonga', PaysID FROM Pays WHERE Code = 'TO'
UNION ALL SELECT 'Region Trinite-et-Tobago', PaysID FROM Pays WHERE Code = 'TT'
UNION ALL SELECT 'Region Tunisie', PaysID FROM Pays WHERE Code = 'TN'
UNION ALL SELECT 'Region Turkmenistan', PaysID FROM Pays WHERE Code = 'TM'
UNION ALL SELECT 'Region Turquie', PaysID FROM Pays WHERE Code = 'TR'
UNION ALL SELECT 'Region Tuvalu', PaysID FROM Pays WHERE Code = 'TV'
UNION ALL SELECT 'Region Ukraine', PaysID FROM Pays WHERE Code = 'UA'
UNION ALL SELECT 'Region Uruguay', PaysID FROM Pays WHERE Code = 'UY'
UNION ALL SELECT 'Region Vanuatu', PaysID FROM Pays WHERE Code = 'VU'
UNION ALL SELECT 'Region Vatican', PaysID FROM Pays WHERE Code = 'VA'
UNION ALL SELECT 'Region Venezuela', PaysID FROM Pays WHERE Code = 'VE'
UNION ALL SELECT 'Region Vietnam', PaysID FROM Pays WHERE Code = 'VN'
UNION ALL SELECT 'Region Yemen', PaysID FROM Pays WHERE Code = 'YE'
UNION ALL SELECT 'Region Zambie', PaysID FROM Pays WHERE Code = 'ZM'
UNION ALL SELECT 'Region Zimbabwe', PaysID FROM Pays WHERE Code = 'ZW';
GO

-- Capital city per country
INSERT INTO Ville (Nom, RegionID)
SELECT 'Kaboul', RegionID FROM Region WHERE Nom = 'Region Afghanistan'
UNION ALL SELECT 'Pretoria', RegionID FROM Region WHERE Nom = 'Region Afrique du Sud'
UNION ALL SELECT 'Tirana', RegionID FROM Region WHERE Nom = 'Region Albanie'
UNION ALL SELECT 'Alger', RegionID FROM Region WHERE Nom = 'Region Algerie'
UNION ALL SELECT 'Berlin', RegionID FROM Region WHERE Nom = 'Region Allemagne'
UNION ALL SELECT 'Andorre-la-Vieille', RegionID FROM Region WHERE Nom = 'Region Andorre'
UNION ALL SELECT 'Luanda', RegionID FROM Region WHERE Nom = 'Region Angola'
UNION ALL SELECT 'Riyad', RegionID FROM Region WHERE Nom = 'Region Arabie Saoudite'
UNION ALL SELECT 'Buenos Aires', RegionID FROM Region WHERE Nom = 'Region Argentine'
UNION ALL SELECT 'Erevan', RegionID FROM Region WHERE Nom = 'Region Armenie'
UNION ALL SELECT 'Canberra', RegionID FROM Region WHERE Nom = 'Region Australie'
UNION ALL SELECT 'Vienne', RegionID FROM Region WHERE Nom = 'Region Autriche'
UNION ALL SELECT 'Bakou', RegionID FROM Region WHERE Nom = 'Region Azerbaidjan'
UNION ALL SELECT 'Nassau', RegionID FROM Region WHERE Nom = 'Region Bahamas'
UNION ALL SELECT 'Manama', RegionID FROM Region WHERE Nom = 'Region Bahrein'
UNION ALL SELECT 'Dacca', RegionID FROM Region WHERE Nom = 'Region Bangladesh'
UNION ALL SELECT 'Bruxelles', RegionID FROM Region WHERE Nom = 'Region Belgique'
UNION ALL SELECT 'Belmopan', RegionID FROM Region WHERE Nom = 'Region Belize'
UNION ALL SELECT 'Porto-Novo', RegionID FROM Region WHERE Nom = 'Region Benin'
UNION ALL SELECT 'Thimphou', RegionID FROM Region WHERE Nom = 'Region Bhoutan'
UNION ALL SELECT 'Minsk', RegionID FROM Region WHERE Nom = 'Region Bielorussie'
UNION ALL SELECT 'Naypyidaw', RegionID FROM Region WHERE Nom = 'Region Birmanie'
UNION ALL SELECT 'Sucre', RegionID FROM Region WHERE Nom = 'Region Bolivie'
UNION ALL SELECT 'Sarajevo', RegionID FROM Region WHERE Nom = 'Region Bosnie-Herzegovine'
UNION ALL SELECT 'Gaborone', RegionID FROM Region WHERE Nom = 'Region Botswana'
UNION ALL SELECT 'Brasilia', RegionID FROM Region WHERE Nom = 'Region Bresil'
UNION ALL SELECT 'Bandar Seri Begawan', RegionID FROM Region WHERE Nom = 'Region Brunei'
UNION ALL SELECT 'Sofia', RegionID FROM Region WHERE Nom = 'Region Bulgarie'
UNION ALL SELECT 'Ouagadougou', RegionID FROM Region WHERE Nom = 'Region Burkina Faso'
UNION ALL SELECT 'Gitega', RegionID FROM Region WHERE Nom = 'Region Burundi'
UNION ALL SELECT 'Phnom Penh', RegionID FROM Region WHERE Nom = 'Region Cambodge'
UNION ALL SELECT 'Yaounde', RegionID FROM Region WHERE Nom = 'Region Cameroun'
UNION ALL SELECT 'Ottawa', RegionID FROM Region WHERE Nom = 'Region Canada'
UNION ALL SELECT 'Praia', RegionID FROM Region WHERE Nom = 'Region Cap-Vert'
UNION ALL SELECT 'Bangui', RegionID FROM Region WHERE Nom = 'Region Republique Centrafricaine'
UNION ALL SELECT 'Santiago', RegionID FROM Region WHERE Nom = 'Region Chili'
UNION ALL SELECT 'Pekin', RegionID FROM Region WHERE Nom = 'Region Chine'
UNION ALL SELECT 'Nicosie', RegionID FROM Region WHERE Nom = 'Region Chypre'
UNION ALL SELECT 'Bogota', RegionID FROM Region WHERE Nom = 'Region Colombie'
UNION ALL SELECT 'Moroni', RegionID FROM Region WHERE Nom = 'Region Comores'
UNION ALL SELECT 'Brazzaville', RegionID FROM Region WHERE Nom = 'Region Congo'
UNION ALL SELECT 'Kinshasa', RegionID FROM Region WHERE Nom = 'Region RD Congo'
UNION ALL SELECT 'Seoul', RegionID FROM Region WHERE Nom = 'Region Coree du Sud'
UNION ALL SELECT 'Pyongyang', RegionID FROM Region WHERE Nom = 'Region Coree du Nord'
UNION ALL SELECT 'San Jose', RegionID FROM Region WHERE Nom = 'Region Costa Rica'
UNION ALL SELECT 'Yamoussoukro', RegionID FROM Region WHERE Nom = 'Region Cote d''Ivoire'
UNION ALL SELECT 'Zagreb', RegionID FROM Region WHERE Nom = 'Region Croatie'
UNION ALL SELECT 'La Havane', RegionID FROM Region WHERE Nom = 'Region Cuba'
UNION ALL SELECT 'Copenhague', RegionID FROM Region WHERE Nom = 'Region Danemark'
UNION ALL SELECT 'Djibouti', RegionID FROM Region WHERE Nom = 'Region Djibouti'
UNION ALL SELECT 'Le Caire', RegionID FROM Region WHERE Nom = 'Region Egypte'
UNION ALL SELECT 'Abou Dabi', RegionID FROM Region WHERE Nom = 'Region Emirats Arabes Unis'
UNION ALL SELECT 'Quito', RegionID FROM Region WHERE Nom = 'Region Equateur'
UNION ALL SELECT 'Asmara', RegionID FROM Region WHERE Nom = 'Region Erythree'
UNION ALL SELECT 'Madrid', RegionID FROM Region WHERE Nom = 'Region Espagne'
UNION ALL SELECT 'Tallinn', RegionID FROM Region WHERE Nom = 'Region Estonie'
UNION ALL SELECT 'Mbabane', RegionID FROM Region WHERE Nom = 'Region Eswatini'
UNION ALL SELECT 'Washington', RegionID FROM Region WHERE Nom = 'Region Etats-Unis'
UNION ALL SELECT 'Addis-Abeba', RegionID FROM Region WHERE Nom = 'Region Ethiopie'
UNION ALL SELECT 'Suva', RegionID FROM Region WHERE Nom = 'Region Fidji'
UNION ALL SELECT 'Helsinki', RegionID FROM Region WHERE Nom = 'Region Finlande'
UNION ALL SELECT 'Paris', RegionID FROM Region WHERE Nom = 'Region France'
UNION ALL SELECT 'Libreville', RegionID FROM Region WHERE Nom = 'Region Gabon'
UNION ALL SELECT 'Banjul', RegionID FROM Region WHERE Nom = 'Region Gambie'
UNION ALL SELECT 'Tbilissi', RegionID FROM Region WHERE Nom = 'Region Georgie'
UNION ALL SELECT 'Accra', RegionID FROM Region WHERE Nom = 'Region Ghana'
UNION ALL SELECT 'Athenes', RegionID FROM Region WHERE Nom = 'Region Grece'
UNION ALL SELECT 'Saint-Georges', RegionID FROM Region WHERE Nom = 'Region Grenade'
UNION ALL SELECT 'Guatemala', RegionID FROM Region WHERE Nom = 'Region Guatemala'
UNION ALL SELECT 'Conakry', RegionID FROM Region WHERE Nom = 'Region Guinee'
UNION ALL SELECT 'Bissau', RegionID FROM Region WHERE Nom = 'Region Guinee-Bissau'
UNION ALL SELECT 'Malabo', RegionID FROM Region WHERE Nom = 'Region Guinee Equatoriale'
UNION ALL SELECT 'Georgetown', RegionID FROM Region WHERE Nom = 'Region Guyana'
UNION ALL SELECT 'Port-au-Prince', RegionID FROM Region WHERE Nom = 'Region Haiti'
UNION ALL SELECT 'Tegucigalpa', RegionID FROM Region WHERE Nom = 'Region Honduras'
UNION ALL SELECT 'Budapest', RegionID FROM Region WHERE Nom = 'Region Hongrie'
UNION ALL SELECT 'New Delhi', RegionID FROM Region WHERE Nom = 'Region Inde'
UNION ALL SELECT 'Jakarta', RegionID FROM Region WHERE Nom = 'Region Indonesie'
UNION ALL SELECT 'Bagdad', RegionID FROM Region WHERE Nom = 'Region Irak'
UNION ALL SELECT 'Teheran', RegionID FROM Region WHERE Nom = 'Region Iran'
UNION ALL SELECT 'Dublin', RegionID FROM Region WHERE Nom = 'Region Irlande'
UNION ALL SELECT 'Reykjavik', RegionID FROM Region WHERE Nom = 'Region Islande'
UNION ALL SELECT 'Jerusalem', RegionID FROM Region WHERE Nom = 'Region Israel'
UNION ALL SELECT 'Rome', RegionID FROM Region WHERE Nom = 'Region Italie'
UNION ALL SELECT 'Kingston', RegionID FROM Region WHERE Nom = 'Region Jamaique'
UNION ALL SELECT 'Tokyo', RegionID FROM Region WHERE Nom = 'Region Japon'
UNION ALL SELECT 'Amman', RegionID FROM Region WHERE Nom = 'Region Jordanie'
UNION ALL SELECT 'Astana', RegionID FROM Region WHERE Nom = 'Region Kazakhstan'
UNION ALL SELECT 'Nairobi', RegionID FROM Region WHERE Nom = 'Region Kenya'
UNION ALL SELECT 'Bichkek', RegionID FROM Region WHERE Nom = 'Region Kirghizistan'
UNION ALL SELECT 'Tarawa', RegionID FROM Region WHERE Nom = 'Region Kiribati'
UNION ALL SELECT 'Koweit', RegionID FROM Region WHERE Nom = 'Region Koweit'
UNION ALL SELECT 'Vientiane', RegionID FROM Region WHERE Nom = 'Region Laos'
UNION ALL SELECT 'Maseru', RegionID FROM Region WHERE Nom = 'Region Lesotho'
UNION ALL SELECT 'Riga', RegionID FROM Region WHERE Nom = 'Region Lettonie'
UNION ALL SELECT 'Beyrouth', RegionID FROM Region WHERE Nom = 'Region Liban'
UNION ALL SELECT 'Monrovia', RegionID FROM Region WHERE Nom = 'Region Liberia'
UNION ALL SELECT 'Tripoli', RegionID FROM Region WHERE Nom = 'Region Libye'
UNION ALL SELECT 'Vaduz', RegionID FROM Region WHERE Nom = 'Region Liechtenstein'
UNION ALL SELECT 'Vilnius', RegionID FROM Region WHERE Nom = 'Region Lituanie'
UNION ALL SELECT 'Luxembourg', RegionID FROM Region WHERE Nom = 'Region Luxembourg'
UNION ALL SELECT 'Skopje', RegionID FROM Region WHERE Nom = 'Region Macedoine du Nord'
UNION ALL SELECT 'Antananarivo', RegionID FROM Region WHERE Nom = 'Region Madagascar'
UNION ALL SELECT 'Kuala Lumpur', RegionID FROM Region WHERE Nom = 'Region Malaisie'
UNION ALL SELECT 'Lilongwe', RegionID FROM Region WHERE Nom = 'Region Malawi'
UNION ALL SELECT 'Male', RegionID FROM Region WHERE Nom = 'Region Maldives'
UNION ALL SELECT 'Bamako', RegionID FROM Region WHERE Nom = 'Region Mali'
UNION ALL SELECT 'La Valette', RegionID FROM Region WHERE Nom = 'Region Malte'
UNION ALL SELECT 'Rabat', RegionID FROM Region WHERE Nom = 'Region Maroc'
UNION ALL SELECT 'Majuro', RegionID FROM Region WHERE Nom = 'Region Iles Marshall'
UNION ALL SELECT 'Port-Louis', RegionID FROM Region WHERE Nom = 'Region Maurice'
UNION ALL SELECT 'Nouakchott', RegionID FROM Region WHERE Nom = 'Region Mauritanie'
UNION ALL SELECT 'Mexico', RegionID FROM Region WHERE Nom = 'Region Mexique'
UNION ALL SELECT 'Palikir', RegionID FROM Region WHERE Nom = 'Region Micronesie'
UNION ALL SELECT 'Chisinau', RegionID FROM Region WHERE Nom = 'Region Moldavie'
UNION ALL SELECT 'Monaco', RegionID FROM Region WHERE Nom = 'Region Monaco'
UNION ALL SELECT 'Oulan-Bator', RegionID FROM Region WHERE Nom = 'Region Mongolie'
UNION ALL SELECT 'Podgorica', RegionID FROM Region WHERE Nom = 'Region Montenegro'
UNION ALL SELECT 'Maputo', RegionID FROM Region WHERE Nom = 'Region Mozambique'
UNION ALL SELECT 'Windhoek', RegionID FROM Region WHERE Nom = 'Region Namibie'
UNION ALL SELECT 'Yaren', RegionID FROM Region WHERE Nom = 'Region Nauru'
UNION ALL SELECT 'Katmandou', RegionID FROM Region WHERE Nom = 'Region Nepal'
UNION ALL SELECT 'Managua', RegionID FROM Region WHERE Nom = 'Region Nicaragua'
UNION ALL SELECT 'Niamey', RegionID FROM Region WHERE Nom = 'Region Niger'
UNION ALL SELECT 'Abuja', RegionID FROM Region WHERE Nom = 'Region Nigeria'
UNION ALL SELECT 'Oslo', RegionID FROM Region WHERE Nom = 'Region Norvege'
UNION ALL SELECT 'Wellington', RegionID FROM Region WHERE Nom = 'Region Nouvelle-Zelande'
UNION ALL SELECT 'Mascate', RegionID FROM Region WHERE Nom = 'Region Oman'
UNION ALL SELECT 'Kampala', RegionID FROM Region WHERE Nom = 'Region Ouganda'
UNION ALL SELECT 'Tachkent', RegionID FROM Region WHERE Nom = 'Region Ouzbekistan'
UNION ALL SELECT 'Islamabad', RegionID FROM Region WHERE Nom = 'Region Pakistan'
UNION ALL SELECT 'Ngerulmud', RegionID FROM Region WHERE Nom = 'Region Palaos'
UNION ALL SELECT 'Panama', RegionID FROM Region WHERE Nom = 'Region Panama'
UNION ALL SELECT 'Port Moresby', RegionID FROM Region WHERE Nom = 'Region Papouasie-Nouvelle-Guinee'
UNION ALL SELECT 'Asuncion', RegionID FROM Region WHERE Nom = 'Region Paraguay'
UNION ALL SELECT 'Amsterdam', RegionID FROM Region WHERE Nom = 'Region Pays-Bas'
UNION ALL SELECT 'Lima', RegionID FROM Region WHERE Nom = 'Region Perou'
UNION ALL SELECT 'Manille', RegionID FROM Region WHERE Nom = 'Region Philippines'
UNION ALL SELECT 'Varsovie', RegionID FROM Region WHERE Nom = 'Region Pologne'
UNION ALL SELECT 'Lisbonne', RegionID FROM Region WHERE Nom = 'Region Portugal'
UNION ALL SELECT 'Doha', RegionID FROM Region WHERE Nom = 'Region Qatar'
UNION ALL SELECT 'Bucarest', RegionID FROM Region WHERE Nom = 'Region Roumanie'
UNION ALL SELECT 'Londres', RegionID FROM Region WHERE Nom = 'Region Royaume-Uni'
UNION ALL SELECT 'Moscou', RegionID FROM Region WHERE Nom = 'Region Russie'
UNION ALL SELECT 'Kigali', RegionID FROM Region WHERE Nom = 'Region Rwanda'
UNION ALL SELECT 'Basseterre', RegionID FROM Region WHERE Nom = 'Region Saint-Christophe-et-Nieves'
UNION ALL SELECT 'Castries', RegionID FROM Region WHERE Nom = 'Region Sainte-Lucie'
UNION ALL SELECT 'Saint-Marin', RegionID FROM Region WHERE Nom = 'Region Saint-Marin'
UNION ALL SELECT 'Kingstown', RegionID FROM Region WHERE Nom = 'Region Saint-Vincent-et-les-Grenadines'
UNION ALL SELECT 'Honiara', RegionID FROM Region WHERE Nom = 'Region Iles Salomon'
UNION ALL SELECT 'San Salvador', RegionID FROM Region WHERE Nom = 'Region Salvador'
UNION ALL SELECT 'Apia', RegionID FROM Region WHERE Nom = 'Region Samoa'
UNION ALL SELECT 'Sao Tome', RegionID FROM Region WHERE Nom = 'Region Sao Tome-et-Principe'
UNION ALL SELECT 'Dakar', RegionID FROM Region WHERE Nom = 'Region Senegal'
UNION ALL SELECT 'Belgrade', RegionID FROM Region WHERE Nom = 'Region Serbie'
UNION ALL SELECT 'Victoria', RegionID FROM Region WHERE Nom = 'Region Seychelles'
UNION ALL SELECT 'Freetown', RegionID FROM Region WHERE Nom = 'Region Sierra Leone'
UNION ALL SELECT 'Singapour', RegionID FROM Region WHERE Nom = 'Region Singapour'
UNION ALL SELECT 'Bratislava', RegionID FROM Region WHERE Nom = 'Region Slovaquie'
UNION ALL SELECT 'Ljubljana', RegionID FROM Region WHERE Nom = 'Region Slovenie'
UNION ALL SELECT 'Mogadiscio', RegionID FROM Region WHERE Nom = 'Region Somalie'
UNION ALL SELECT 'Khartoum', RegionID FROM Region WHERE Nom = 'Region Soudan'
UNION ALL SELECT 'Djouba', RegionID FROM Region WHERE Nom = 'Region Soudan du Sud'
UNION ALL SELECT 'Sri Jayewardenepura Kotte', RegionID FROM Region WHERE Nom = 'Region Sri Lanka'
UNION ALL SELECT 'Stockholm', RegionID FROM Region WHERE Nom = 'Region Suede'
UNION ALL SELECT 'Berne', RegionID FROM Region WHERE Nom = 'Region Suisse'
UNION ALL SELECT 'Paramaribo', RegionID FROM Region WHERE Nom = 'Region Suriname'
UNION ALL SELECT 'Damas', RegionID FROM Region WHERE Nom = 'Region Syrie'
UNION ALL SELECT 'Douchanbe', RegionID FROM Region WHERE Nom = 'Region Tadjikistan'
UNION ALL SELECT 'Dodoma', RegionID FROM Region WHERE Nom = 'Region Tanzanie'
UNION ALL SELECT 'N''Djamena', RegionID FROM Region WHERE Nom = 'Region Tchad'
UNION ALL SELECT 'Prague', RegionID FROM Region WHERE Nom = 'Region Tchequie'
UNION ALL SELECT 'Bangkok', RegionID FROM Region WHERE Nom = 'Region Thailande'
UNION ALL SELECT 'Dili', RegionID FROM Region WHERE Nom = 'Region Timor Oriental'
UNION ALL SELECT 'Lome', RegionID FROM Region WHERE Nom = 'Region Togo'
UNION ALL SELECT 'Nuku''alofa', RegionID FROM Region WHERE Nom = 'Region Tonga'
UNION ALL SELECT 'Port d''Espagne', RegionID FROM Region WHERE Nom = 'Region Trinite-et-Tobago'
UNION ALL SELECT 'Tunis', RegionID FROM Region WHERE Nom = 'Region Tunisie'
UNION ALL SELECT 'Achgabat', RegionID FROM Region WHERE Nom = 'Region Turkmenistan'
UNION ALL SELECT 'Ankara', RegionID FROM Region WHERE Nom = 'Region Turquie'
UNION ALL SELECT 'Funafuti', RegionID FROM Region WHERE Nom = 'Region Tuvalu'
UNION ALL SELECT 'Kiev', RegionID FROM Region WHERE Nom = 'Region Ukraine'
UNION ALL SELECT 'Montevideo', RegionID FROM Region WHERE Nom = 'Region Uruguay'
UNION ALL SELECT 'Port-Vila', RegionID FROM Region WHERE Nom = 'Region Vanuatu'
UNION ALL SELECT 'Vatican', RegionID FROM Region WHERE Nom = 'Region Vatican'
UNION ALL SELECT 'Caracas', RegionID FROM Region WHERE Nom = 'Region Venezuela'
UNION ALL SELECT 'Hanoi', RegionID FROM Region WHERE Nom = 'Region Vietnam'
UNION ALL SELECT 'Sanaa', RegionID FROM Region WHERE Nom = 'Region Yemen'
UNION ALL SELECT 'Lusaka', RegionID FROM Region WHERE Nom = 'Region Zambie'
UNION ALL SELECT 'Harare', RegionID FROM Region WHERE Nom = 'Region Zimbabwe';
GO

-- ---- Expanded currencies (major world currencies, ISO 4217) ----
DELETE FROM Currency;
INSERT INTO Currency (CurrencyCode, Nom, Symbole) VALUES
('XAF','Franc CFA (BEAC)','FCFA'),('XOF','Franc CFA (BCEAO)','FCFA'),
('USD','Dollar americain','$'),('EUR','Euro','EUR'),('GBP','Livre sterling','GBP'),
('NGN','Naira nigerian','NGN'),('ZAR','Rand sud-africain','ZAR'),('EGP','Livre egyptienne','EGP'),
('MAD','Dirham marocain','MAD'),('DZD','Dinar algerien','DZD'),('TND','Dinar tunisien','TND'),
('KES','Shilling kenyan','KES'),('GHS','Cedi ghaneen','GHS'),('CDF','Franc congolais','CDF'),
('ETB','Birr ethiopien','ETB'),('CNY','Yuan chinois','CNY'),('JPY','Yen japonais','JPY'),
('INR','Roupie indienne','INR'),('KRW','Won sud-coreen','KRW'),('AUD','Dollar australien','AUD'),
('CAD','Dollar canadien','CAD'),('CHF','Franc suisse','CHF'),('SEK','Couronne suedoise','SEK'),
('NOK','Couronne norvegienne','NOK'),('DKK','Couronne danoise','DKK'),('PLN','Zloty polonais','PLN'),
('RON','Leu roumain','RON'),('TRY','Livre turque','TRY'),('RUB','Rouble russe','RUB'),
('BRL','Real bresilien','BRL'),('MXN','Peso mexicain','MXN'),('ARS','Peso argentin','ARS'),
('AED','Dirham des EAU','AED'),('SAR','Riyal saoudien','SAR'),('QAR','Riyal qatari','QAR'),
('ILS','Shekel israelien','ILS'),('THB','Baht thailandais','THB'),('MYR','Ringgit malaisien','MYR'),
('IDR','Roupie indonesienne','IDR'),('PHP','Peso philippin','PHP'),('VND','Dong vietnamien','VND'),
('PKR','Roupie pakistanaise','PKR'),('BDT','Taka bangladais','BDT'),('NZD','Dollar neo-zelandais','NZD');
GO

-- ---- Expanded timezones (representative set covering every UTC offset) ----
DELETE FROM TimeZoneRef;
INSERT INTO TimeZoneRef (Code, Label, UtcOffset) VALUES
('Pacific/Midway','Midway','-11:00'),('Pacific/Honolulu','Honolulu','-10:00'),
('America/Anchorage','Anchorage','-09:00'),('America/Los_Angeles','Los Angeles','-08:00'),
('America/Denver','Denver','-07:00'),('America/Chicago','Chicago','-06:00'),
('America/Mexico_City','Mexico','-06:00'),('America/New_York','New York','-05:00'),
('America/Bogota','Bogota','-05:00'),('America/Caracas','Caracas','-04:00'),
('America/Santiago','Santiago','-04:00'),('America/Sao_Paulo','Sao Paulo','-03:00'),
('America/Argentina/Buenos_Aires','Buenos Aires','-03:00'),('Atlantic/Azores','Acores','-01:00'),
('UTC','UTC','+00:00'),('Europe/London','Londres','+00:00'),('Africa/Abidjan','Abidjan','+00:00'),
('Africa/Casablanca','Casablanca','+00:00'),('Africa/Dakar','Dakar','+00:00'),
('Europe/Paris','Paris','+01:00'),('Africa/Douala','Douala','+01:00'),
('Africa/Lagos','Lagos','+01:00'),('Africa/Tunis','Tunis','+01:00'),
('Europe/Athens','Athenes','+02:00'),('Africa/Cairo','Le Caire','+02:00'),
('Africa/Johannesburg','Johannesburg','+02:00'),('Africa/Kinshasa','Kinshasa','+01:00'),
('Europe/Moscow','Moscou','+03:00'),('Africa/Nairobi','Nairobi','+03:00'),
('Asia/Riyadh','Riyad','+03:00'),('Asia/Dubai','Dubai','+04:00'),
('Asia/Tehran','Teheran','+03:30'),('Asia/Karachi','Karachi','+05:00'),
('Asia/Kolkata','New Delhi','+05:30'),('Asia/Dhaka','Dacca','+06:00'),
('Asia/Bangkok','Bangkok','+07:00'),('Asia/Shanghai','Pekin/Shanghai','+08:00'),
('Asia/Singapore','Singapour','+08:00'),('Asia/Tokyo','Tokyo','+09:00'),
('Asia/Seoul','Seoul','+09:00'),('Australia/Sydney','Sydney','+10:00'),
('Pacific/Auckland','Auckland','+12:00');
GO
