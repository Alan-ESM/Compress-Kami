# Compress-Kami

# En quoi consiste le projet ?

# Compress-Kami est une application C# / WPF permettant de :

# 

# •	compresser un dossier dans un format propriétaire .keyce ;

# •	décompresser un fichier .keyce ;

# •	créer un ZIP ;

# •	extraire un ZIP.

# 

# La compression propriétaire utilise un arbre de Huffman construit à partir des fréquences des octets des fichiers du dossier. Le format conserve les chemins relatifs, les dossiers, les tailles et certains types de fichiers.

# 

# Ce qui semble déjà réalisé

# •	Interface WPF avec boutons de compression et décompression.

# •	Traitement en tâche de fond pour ne pas bloquer l’interface.

# •	Barre/progression et messages de succès ou d’erreur.

# •	Compression et décompression de dossiers, y compris les dossiers vides.

# •	Support de ZIP via System.IO.Compression.

# •	Signature de format .keyce et contrôle élémentaire avec IsKeyCeFile.

# •	Prise en compte des images, vidéos, documents et fichiers binaires.

# 

# Ce qui manque ou doit être corrigé

# •	README.md ne contient pratiquement pas de documentation : il faut expliquer le format, la compilation, l’utilisation et les limites.

# •	La méthode OptimizeImageData ne fait actuellement aucune optimisation : elle retourne les octets originaux. Le support multimédia annoncé est donc surtout déclaratif.

# •	Les vidéos et documents ne bénéficient pas d’une optimisation spécifique ; ils sont traités par Huffman comme des données binaires.

# •	Le format ne semble pas stocker explicitement le nombre de bits valides du dernier octet. La taille originale limite la lecture, mais il faut ajouter des tests de round-trip sur tous les types de fichiers.

# •	Le cas d’un fichier contenant un seul octet distinct doit être testé : un arbre Huffman à une seule feuille peut produire un code vide, ce qui risque de donner une décompression incorrecte.

# •	Les chemins relatifs doivent être contrôlés contre les attaques de type .. lors de la décompression afin d’empêcher l’écriture hors du dossier de destination.

# •	Les fichiers non lisibles sont ignorés pendant la collecte : l’utilisateur doit recevoir une liste claire des fichiers non inclus.

# •	Il manque une annulation de tâche, une progression en pourcentage fiable et une protection contre l’écrasement de fichiers existants.

# •	Il faut ajouter des tests automatisés : dossier vide, fichier vide, gros fichier, nom Unicode, sous-dossiers, fichier binaire, image, fichier composé d’un seul octet répété et archive corrompue.

# •	La gestion de version lit la version mais ne semble pas refuser clairement les versions incompatibles.

# 

# Niveau estimé

# Prototype avancé, avec une interface utilisable, mais le format .keyce doit être stabilisé et testé avant une utilisation fiable.



