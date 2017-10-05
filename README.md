# Eventing
Common event sourcing code 

Help:
For publishing a self hosted Web Api in windows:

register in the console app the url like http://+:84 instead of http://localhost:84

type in cmd 'whoami' for the user name:

For publishing:
netsh http add urlacl url=http://+:2113/ user=DOMAIN\username
