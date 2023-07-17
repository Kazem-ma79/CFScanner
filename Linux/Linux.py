import sys
import argparse
import requests
import ipaddress
from concurrent.futures import ThreadPoolExecutor, as_completed

# ANSI escape sequences for colors
GREEN = '\033[92m'
RED = '\033[91m'
YELLOW = '\033[93m'
RESET = '\033[0m'

def print_good_ip(ip):
    print(f'{GREEN}[+] Good IP => {ip}{RESET}')
    with open('good.txt', 'a') as file:
        file.write(ip + '\n')

def print_filtered_ip(ip):
    print(f'{RED}[-] Filtered IP => {ip}{RESET}')

def print_unknown_ip(ip, location):
    print(f'{YELLOW}[X] Unknown redirect from {ip} to => {location}{RESET}')


def scan(hostname, port, thread):
    with open('cdn.txt', 'r') as f:
        cdn_ips = f.read().splitlines()
    with ThreadPoolExecutor(max_workers=thread) as executor:
        for ip in cdn_ips:
            try:
                future = executor.submit(check, hostname, port, ip)
            except KeyboardInterrupt:
                executor.shutdown(wait=False, cancel_futures=True)
                print("Scan process cancelled by user")


def check(hostname, port, ip):
    url = f'http://{ip}:{port}/'
    headers = {
        'host': hostname
    }
    try:
        response = requests.get(url, headers=headers, timeout=5, allow_redirects=False)
        if response.status_code == 400:
            print_good_ip(ip)
        elif response.status_code == 301 or response.status_code == 302:
            if ipaddress.IPv4Address(response.headers['Location'].split('/')[2]) in ipaddress.ip_network('10.10.34.0/24'):
                print_filtered_ip(ip)
            else:
                print_unknown_ip(ip, response.headers["Location"])
    except requests.exceptions.Timeout:
        pass
    except Exception as e:
        print(f'Error connecting to {url}: {e}')


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=["scan", "check"], help="Mode: scan or check")
    parser.add_argument("--hostname", help="Hostname")
    parser.add_argument("--port", type=int, help="Port")
    parser.add_argument("--thread", type=int, help="Number of threads (only for scan mode)")
    parser.add_argument("--ip", help="IP address (only for check mode)")
    args = parser.parse_args()

    if args.mode is None or args.hostname is None or args.port is None:
        print("Insufficient arguments.")
        sys.exit(1)

    if args.mode == "scan":
        if args.thread is None:
            print("Thread argument missing for scan mode.")
            sys.exit(1)
        scan(args.hostname, args.port, args.thread)
    elif args.mode == "check":
        if args.ip is None:
            print("IP argument missing for check mode.")
            sys.exit(1)
        check(args.hostname, args.port, args.ip)

